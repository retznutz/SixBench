using System.Net.Sockets;
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
/// Delivers debug console output and state changes to viewers (implemented by the API over SignalR).
/// </summary>
public interface IRokuConsoleNotifier
{
    /// <summary>Sends output to everyone watching that Roku's console.</summary>
    /// <param name="output">The output.</param>
    /// <returns>A task.</returns>
    Task OutputAsync(RokuConsoleOutputDto output);

    /// <summary>Sends a connection state change to everyone watching that Roku's console.</summary>
    /// <param name="status">The new state.</param>
    /// <returns>A task.</returns>
    Task StatusAsync(RokuConsoleStatusDto status);
}

/// <summary>
/// The BrightScript debug console (telnet, port 8085). The Roku allows few connections to it, so the server
/// keeps one per Roku and shares it with every viewer, reconnecting while anyone is watching.
/// </summary>
public interface IRokuDebugConsoleManager
{
    /// <summary>Starts watching a Roku's console, connecting if nobody else is.</summary>
    /// <param name="rokuId">Saved device id.</param>
    /// <param name="viewerId">Viewer key (the SignalR connection id).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Current state and recent output.</returns>
    /// <exception cref="NotFoundException">No such device.</exception>
    Task<RokuConsoleSnapshotDto> SubscribeAsync(int rokuId, string viewerId, CancellationToken ct = default);

    /// <summary>Stops watching; the connection closes when the last viewer leaves.</summary>
    /// <param name="rokuId">Saved device id.</param>
    /// <param name="viewerId">Viewer key.</param>
    void Unsubscribe(int rokuId, string viewerId);

    /// <summary>Stops every console a viewer is watching (it disconnected).</summary>
    /// <param name="viewerId">Viewer key.</param>
    void UnsubscribeAll(string viewerId);

    /// <summary>Types a line into the console (for example <c>bt</c> or <c>cont</c> at a debugger prompt).</summary>
    /// <param name="rokuId">Saved device id.</param>
    /// <param name="line">The line, without a line ending.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    /// <exception cref="RokuRequestRejectedException">The console is not connected.</exception>
    Task SendAsync(int rokuId, string line, CancellationToken ct = default);
}

/// <summary>
/// Default <see cref="IRokuDebugConsoleManager"/>.
/// </summary>
/// <param name="scopes">Scope factory (the Roku repository is scoped).</param>
/// <param name="notifier">Delivers output to viewers.</param>
/// <param name="options">Roku options.</param>
/// <param name="logger">Logger.</param>
public sealed class RokuDebugConsoleManager(
    IServiceScopeFactory scopes,
    IRokuConsoleNotifier notifier,
    IOptions<RokuOptions> options,
    ILogger<RokuDebugConsoleManager> logger) : IRokuDebugConsoleManager, IDisposable
{
    /// <summary>State while opening the connection.</summary>
    public const string Connecting = "Connecting";

    /// <summary>State while connected.</summary>
    public const string Connected = "Connected";

    /// <summary>State while waiting to reconnect.</summary>
    public const string Disconnected = "Disconnected";

    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan[] RetryDelays = [TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)];

    private readonly Lock _gate = new();
    private readonly Dictionary<int, Session> _sessions = [];

    /// <inheritdoc />
    public async Task<RokuConsoleSnapshotDto> SubscribeAsync(int rokuId, string viewerId, CancellationToken ct = default)
    {
        string host;
        await using (var scope = scopes.CreateAsyncScope())
        {
            var device = await scope.ServiceProvider.GetRequiredService<IRokuDeviceRepository>().GetAsync(rokuId, ct)
                ?? throw new NotFoundException($"Roku device {rokuId} was not found.");
            host = device.IpAddress;
        }

        Session? session;
        var started = false;
        lock (_gate)
        {
            if (!_sessions.TryGetValue(rokuId, out session))
            {
                session = new Session(rokuId, host, options.Value.DebugConsolePort);
                _sessions[rokuId] = session;
                started = true;
            }

            session.Viewers.Add(viewerId);
        }

        if (started)
        {
            _ = Task.Run(() => RunAsync(session), CancellationToken.None);
        }

        return session.Snapshot();
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
    public async Task SendAsync(int rokuId, string line, CancellationToken ct = default)
    {
        Session? session;
        lock (_gate)
        {
            _sessions.TryGetValue(rokuId, out session);
        }

        var stream = session?.Stream
            ?? throw new RokuRequestRejectedException("The debug console is not connected.");
        var bytes = Encoding.UTF8.GetBytes(line.ReplaceLineEndings(string.Empty) + "\r\n");
        await session.WriteLock.WaitAsync(ct);
        try
        {
            await stream.WriteAsync(bytes, ct);
            await stream.FlushAsync(ct);
        }
        catch (IOException ex)
        {
            throw new RokuRequestRejectedException($"The debug console connection was lost: {ex.Message}");
        }
        finally
        {
            session.WriteLock.Release();
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

    /// <summary>
    /// Removes telnet negotiation (IAC sequences). 0xFF never occurs in UTF-8 text, so this cannot eat output.
    /// </summary>
    /// <param name="data">Bytes as received.</param>
    /// <returns>The text bytes.</returns>
    public static byte[] StripTelnet(ReadOnlySpan<byte> data)
    {
        if (!data.Contains((byte)0xFF))
        {
            return data.ToArray();
        }

        var result = new List<byte>(data.Length);
        for (var i = 0; i < data.Length; i++)
        {
            if (data[i] != 0xFF)
            {
                result.Add(data[i]);
                continue;
            }

            // IAC WILL/WONT/DO/DONT take an option byte; other commands are two bytes.
            i += i + 1 < data.Length && data[i + 1] is >= 0xFB and <= 0xFE ? 2 : 1;
        }

        return [.. result];
    }

    private async Task RunAsync(Session session)
    {
        var ct = session.Stopping.Token;
        var failures = 0;
        while (!ct.IsCancellationRequested)
        {
            string message;
            await SetStatusAsync(session, Connecting, null);
            try
            {
                using var tcp = new TcpClient();
                using (var connect = CancellationTokenSource.CreateLinkedTokenSource(ct))
                {
                    connect.CancelAfter(ConnectTimeout);
                    await tcp.ConnectAsync(session.Host, session.Port, connect.Token);
                }

                var stream = tcp.GetStream();
                session.Stream = stream;
                failures = 0;
                await SetStatusAsync(session, Connected, null);

                var decoder = Encoding.UTF8.GetDecoder();
                var buffer = new byte[8192];
                var chars = new char[Encoding.UTF8.GetMaxCharCount(buffer.Length)];
                int read;
                while ((read = await stream.ReadAsync(buffer, ct)) > 0)
                {
                    var text = StripTelnet(buffer.AsSpan(0, read));
                    var count = decoder.GetChars(text, 0, text.Length, chars, 0);
                    if (count > 0)
                    {
                        await OutputAsync(session, new string(chars, 0, count));
                    }
                }

                message = "The Roku closed the debug console.";
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (ex is SocketException or IOException or OperationCanceledException)
            {
                failures++;
                message = ex is OperationCanceledException
                    ? $"Could not reach {session.Host}:{session.Port} in time."
                    : $"Could not connect to {session.Host}:{session.Port}: {ex.Message}";
                message += " The debug console needs developer mode on, and allows only one connection (close other telnet sessions or debuggers).";
            }
            finally
            {
                session.Stream = null;
            }

            await SetStatusAsync(session, Disconnected, message);
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

    private async Task OutputAsync(Session session, string text)
    {
        var output = session.Append(text, options.Value.DebugConsoleBacklogChars);
        try
        {
            await notifier.OutputAsync(output);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not deliver debug console output for Roku {RokuId}", session.RokuId);
        }
    }

    private async Task SetStatusAsync(Session session, string state, string? message)
    {
        var status = session.SetStatus(state, message);
        if (session.Stopping.IsCancellationRequested)
        {
            return;
        }

        try
        {
            await notifier.StatusAsync(status);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not deliver debug console status for Roku {RokuId}", session.RokuId);
        }
    }

    private sealed class Session(int rokuId, string host, int port)
    {
        private readonly Lock _lock = new();
        private readonly StringBuilder _backlog = new();
        private long _sequence;
        private RokuConsoleStatusDto _status = new(rokuId, Connecting, null);

        public int RokuId { get; } = rokuId;

        public string Host { get; } = host;

        public int Port { get; } = port;

        public HashSet<string> Viewers { get; } = [];

        public CancellationTokenSource Stopping { get; } = new();

        public SemaphoreSlim WriteLock { get; } = new(1, 1);

        private volatile NetworkStream? _stream;

        public NetworkStream? Stream
        {
            get => _stream;
            set => _stream = value;
        }

        public RokuConsoleOutputDto Append(string text, int maxChars)
        {
            lock (_lock)
            {
                _backlog.Append(text);
                if (_backlog.Length > maxChars)
                {
                    _backlog.Remove(0, _backlog.Length - maxChars);
                }

                return new RokuConsoleOutputDto(RokuId, ++_sequence, text);
            }
        }

        public RokuConsoleStatusDto SetStatus(string state, string? message)
        {
            lock (_lock)
            {
                return _status = new RokuConsoleStatusDto(RokuId, state, message);
            }
        }

        public RokuConsoleSnapshotDto Snapshot()
        {
            lock (_lock)
            {
                return new RokuConsoleSnapshotDto(_status, _backlog.ToString(), _sequence);
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
