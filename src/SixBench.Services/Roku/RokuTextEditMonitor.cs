using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixBench.Common.Dtos;
using SixBench.Common.Options;
using SixBench.Data.Repositories;
using SixBench.Services.Exceptions;

namespace SixBench.Services.Roku;

/// <summary>
/// Delivers keyboard state changes to viewers (implemented by the API over SignalR).
/// </summary>
public interface IRokuTextEditNotifier
{
    /// <summary>Sends a change to everyone watching that Roku.</summary>
    /// <param name="state">The new state.</param>
    /// <returns>A task.</returns>
    Task StateAsync(RokuTextEditStateDto state);
}

/// <summary>
/// Watches whether a Roku's on-screen keyboard is open, so the browser can open its text box when a text field on
/// the Roku is selected. Keeps one ECP-2 websocket per Roku, shared by every viewer, while anyone is watching.
/// </summary>
public interface IRokuTextEditMonitor
{
    /// <summary>Starts watching a Roku's keyboard, connecting if nobody else is.</summary>
    /// <param name="rokuId">Saved device id.</param>
    /// <param name="viewerId">Viewer key (the SignalR connection id).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The current state; changes follow through <see cref="IRokuTextEditNotifier"/>.</returns>
    /// <exception cref="NotFoundException">No such device.</exception>
    Task<RokuTextEditStateDto> SubscribeAsync(int rokuId, string viewerId, CancellationToken ct = default);

    /// <summary>Stops watching; the connection closes when the last viewer leaves.</summary>
    /// <param name="rokuId">Saved device id.</param>
    /// <param name="viewerId">Viewer key.</param>
    void Unsubscribe(int rokuId, string viewerId);

    /// <summary>Stops everything a viewer is watching (it disconnected).</summary>
    /// <param name="viewerId">Viewer key.</param>
    void UnsubscribeAll(string viewerId);
}

/// <summary>
/// Default <see cref="IRokuTextEditMonitor"/>. It subscribes to the Roku's <c>textedit-*</c> events and also asks
/// for the state every <see cref="RokuOptions.TextEditPollMs"/>, since not every keyboard raises events.
/// </summary>
/// <param name="scopes">Scope factory (the Roku repository is scoped).</param>
/// <param name="notifier">Delivers changes to viewers.</param>
/// <param name="options">Roku options.</param>
/// <param name="logger">Logger.</param>
public sealed class RokuTextEditMonitor(
    IServiceScopeFactory scopes,
    IRokuTextEditNotifier notifier,
    IOptions<RokuOptions> options,
    ILogger<RokuTextEditMonitor> logger) : IRokuTextEditMonitor, IDisposable
{
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan[] RetryDelays =
        [TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60)];

    private readonly Lock _gate = new();
    private readonly Dictionary<int, Session> _sessions = [];

    /// <inheritdoc />
    public async Task<RokuTextEditStateDto> SubscribeAsync(int rokuId, string viewerId, CancellationToken ct = default)
    {
        string host;
        int port;
        await using (var scope = scopes.CreateAsyncScope())
        {
            var device = await scope.ServiceProvider.GetRequiredService<IRokuDeviceRepository>().GetAsync(rokuId, ct)
                ?? throw new NotFoundException($"Roku device {rokuId} was not found.");
            host = device.IpAddress;
            port = device.Port;
        }

        if (!options.Value.TextEditMonitorEnabled)
        {
            return new RokuTextEditStateDto(rokuId, false, false, null, null);
        }

        Session? session;
        var started = false;
        lock (_gate)
        {
            if (!_sessions.TryGetValue(rokuId, out session))
            {
                session = new Session(rokuId, host, port);
                _sessions[rokuId] = session;
                started = true;
            }

            session.Viewers.Add(viewerId);
        }

        if (started)
        {
            _ = Task.Run(() => RunAsync(session), CancellationToken.None);
        }

        return session.State;
    }

    /// <inheritdoc />
    public void Unsubscribe(int rokuId, string viewerId)
    {
        lock (_gate)
        {
            if (_sessions.TryGetValue(rokuId, out var session) && session.Viewers.Remove(viewerId) && session.Viewers.Count == 0)
            {
                _sessions.Remove(rokuId);
                session.Stop();
            }
        }
    }

    /// <inheritdoc />
    public void UnsubscribeAll(string viewerId)
    {
        lock (_gate)
        {
            foreach (var (rokuId, session) in _sessions.ToList())
            {
                if (session.Viewers.Remove(viewerId) && session.Viewers.Count == 0)
                {
                    _sessions.Remove(rokuId);
                    session.Stop();
                }
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_gate)
        {
            foreach (var session in _sessions.Values)
            {
                session.Stop();
            }

            _sessions.Clear();
        }
    }

    private async Task RunAsync(Session session)
    {
        var ct = session.Stopping.Token;
        var failures = 0;
        while (!ct.IsCancellationRequested)
        {
            var authenticated = false;
            try
            {
                using var ws = new ClientWebSocket();
                ws.Options.AddSubProtocol(RokuEcp2Protocol.SubProtocol);
                using (var connect = CancellationTokenSource.CreateLinkedTokenSource(ct))
                {
                    connect.CancelAfter(ConnectTimeout);
                    await ws.ConnectAsync(new Uri($"ws://{session.Host}:{session.Port}/{RokuEcp2Protocol.Path}"), connect.Token);
                }

                authenticated = await ListenAsync(session, ws, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (ex is WebSocketException or IOException or OperationCanceledException)
            {
                logger.LogDebug(ex, "Keyboard monitor for Roku {RokuId} at {Host} disconnected", session.RokuId, session.Host);
            }

            failures = authenticated ? 0 : failures + 1;
            await PublishAsync(session, session.State with { Available = false, Open = false, TextEditId = null, Text = null });
            try
            {
                await Task.Delay(RetryDelays[Math.Min(failures, RetryDelays.Length - 1)], ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        session.Stopping.Dispose();
    }

    /// <summary>Authenticates, subscribes and follows the keyboard state until the socket closes.</summary>
    /// <returns>Whether the Roku accepted the authentication.</returns>
    private async Task<bool> ListenAsync(Session session, ClientWebSocket ws, CancellationToken ct)
    {
        var sendLock = new SemaphoreSlim(1, 1);
        long requestId = 0;
        async Task SendAsync(Func<long, string> build)
        {
            await sendLock.WaitAsync(ct);
            try
            {
                await ws.SendAsync(Encoding.UTF8.GetBytes(build(++requestId)), WebSocketMessageType.Text, true, ct);
            }
            finally
            {
                sendLock.Release();
            }
        }

        var authenticated = false;
        using var polling = CancellationTokenSource.CreateLinkedTokenSource(ct);
        Task? poller = null;
        try
        {
            while (await ReceiveAsync(ws, ct) is { } text)
            {
                var message = RokuEcp2Protocol.Parse(text);
                if (message is null)
                {
                    continue;
                }

                if (message.Notify == "authenticate" && message.Challenge is { } challenge)
                {
                    await SendAsync(id => RokuEcp2Protocol.Authenticate(id, challenge, options.Value.EcpSessionKey));
                }
                else if (message.Response == "authenticate")
                {
                    if (!message.Ok)
                    {
                        logger.LogWarning(
                            "Roku {RokuId} at {Host} rejected the ECP-2 authentication (status {Status}); the text box will not open automatically. Check Roku:EcpSessionKey.",
                            session.RokuId, session.Host, message.Status);
                        return false;
                    }

                    authenticated = true;
                    await SendAsync(RokuEcp2Protocol.RequestTextEditEvents);
                    await SendAsync(RokuEcp2Protocol.QueryTextEditState);
                    if (options.Value.TextEditPollMs > 0)
                    {
                        poller = PollAsync(TimeSpan.FromMilliseconds(options.Value.TextEditPollMs), () => SendAsync(RokuEcp2Protocol.QueryTextEditState), polling.Token);
                    }
                }
                else if (message.Response == "request-events" && !message.Ok)
                {
                    logger.LogInformation(
                        "Roku {RokuId} does not offer keyboard events (status {Status}); asking for the keyboard state instead.",
                        session.RokuId, message.Status);
                }
                else if (message.Response == "query-textedit-state" && message.Ok
                    && RokuEcp2Protocol.ParseTextEditState(message.Content) is { } field)
                {
                    await PublishAsync(session, new RokuTextEditStateDto(session.RokuId, true, field.TextEditId is not null, field.TextEditId, field.Text));
                }
                else if (message.Notify is { } notify && RokuEcp2Protocol.TextEditEvents.Contains(notify))
                {
                    // The event's own fields are undocumented; the query answers authoritatively.
                    await SendAsync(RokuEcp2Protocol.QueryTextEditState);
                }
            }

            return authenticated;
        }
        finally
        {
            await polling.CancelAsync();
            if (poller is not null)
            {
                await poller.ContinueWith(_ => { }, TaskScheduler.Default);
            }

            sendLock.Dispose();
        }
    }

    private static async Task PollAsync(TimeSpan interval, Func<Task> query, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(ct))
        {
            await query();
        }
    }

    /// <summary>Reads one text message, or null when the socket closes.</summary>
    private static async Task<string?> ReceiveAsync(ClientWebSocket ws, CancellationToken ct)
    {
        var buffer = new byte[8192];
        using var message = new MemoryStream();
        while (true)
        {
            var result = await ws.ReceiveAsync(buffer, ct);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            message.Write(buffer, 0, result.Count);
            if (result.EndOfMessage)
            {
                return Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length);
            }
        }
    }

    private async Task PublishAsync(Session session, RokuTextEditStateDto state)
    {
        if (!session.TrySetState(state) || session.Stopping.IsCancellationRequested)
        {
            return;
        }

        try
        {
            await notifier.StateAsync(state);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not deliver keyboard state for Roku {RokuId}", session.RokuId);
        }
    }

    private sealed class Session(int rokuId, string host, int port)
    {
        private readonly Lock _lock = new();
        private RokuTextEditStateDto _state = new(rokuId, false, false, null, null);

        public int RokuId { get; } = rokuId;

        public string Host { get; } = host;

        public int Port { get; } = port;

        public HashSet<string> Viewers { get; } = [];

        public CancellationTokenSource Stopping { get; } = new();

        public RokuTextEditStateDto State
        {
            get
            {
                lock (_lock)
                {
                    return _state;
                }
            }
        }

        /// <summary>Records a state.</summary>
        /// <returns>Whether it differs from the last one.</returns>
        public bool TrySetState(RokuTextEditStateDto state)
        {
            lock (_lock)
            {
                if (state == _state)
                {
                    return false;
                }

                _state = state;
                return true;
            }
        }

        public void Stop()
        {
            try
            {
                Stopping.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Already stopped.
            }
        }
    }
}
