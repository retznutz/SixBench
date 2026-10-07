using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixBench.Common.Dtos;
using SixBench.Common.Options;
using SixBench.Services.Capture;

namespace SixBench.Services.Streaming;

/// <summary>
/// A subscriber's hold on a capture session; return it with <see cref="ICaptureSessionManager.ReleaseAsync"/>.
/// </summary>
/// <param name="Session">The session.</param>
/// <param name="Subscriber">This client's subscriber.</param>
public sealed record CaptureLease(CaptureSession Session, StreamSubscriber Subscriber);

/// <summary>
/// Owns the capture sessions: one per device, started on first use and stopped after an idle period.
/// </summary>
public interface ICaptureSessionManager
{
    /// <summary>
    /// Subscribes to a device's stream, starting capture if needed.
    /// </summary>
    /// <param name="stableId">Capture device stable id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The lease.</returns>
    /// <exception cref="Exceptions.NotFoundException">The device is not connected.</exception>
    Task<CaptureLease> AcquireAsync(string stableId, CancellationToken ct = default);

    /// <summary>
    /// Unsubscribes; capture stops after the idle timeout when no subscribers remain.
    /// </summary>
    /// <param name="lease">The lease from <see cref="AcquireAsync"/>.</param>
    /// <returns>A task.</returns>
    Task ReleaseAsync(CaptureLease lease);

    /// <summary>
    /// Ends a device's session (clients receive <c>stream_ended</c> and reconnect with new settings).
    /// </summary>
    /// <param name="stableId">Capture device stable id.</param>
    /// <param name="reason">Reason sent to clients.</param>
    /// <returns>A task.</returns>
    Task RestartAsync(string stableId, string reason);

    /// <summary>
    /// Returns diagnostics for all active sessions.
    /// </summary>
    /// <returns>Session snapshots.</returns>
    IReadOnlyList<StreamSessionDto> GetSessions();
}

/// <summary>
/// Default <see cref="ICaptureSessionManager"/>. Registered as a singleton and hosted service so that
/// all ffmpeg processes are stopped on shutdown.
/// </summary>
/// <param name="scopeFactory">Scope factory (device resolution uses scoped repositories).</param>
/// <param name="factory">Media source factory.</param>
/// <param name="options">Streaming options.</param>
/// <param name="timeProvider">Clock for idle timers.</param>
/// <param name="loggerFactory">Logger factory.</param>
public sealed class CaptureSessionManager(
    IServiceScopeFactory scopeFactory,
    IMediaSourceFactory factory,
    IOptions<StreamingOptions> options,
    TimeProvider timeProvider,
    ILoggerFactory loggerFactory) : ICaptureSessionManager, IHostedService
{
    private readonly object _lock = new();
    private readonly Dictionary<string, Entry> _sessions = new(StringComparer.Ordinal);
    private readonly ILogger _logger = loggerFactory.CreateLogger<CaptureSessionManager>();

    /// <inheritdoc />
    public async Task<CaptureLease> AcquireAsync(string stableId, CancellationToken ct = default)
    {
        if (TrySubscribeExisting(stableId) is { } existing)
        {
            return existing;
        }

        CaptureSource source;
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            source = await scope.ServiceProvider.GetRequiredService<ICaptureDeviceService>().ResolveSourceAsync(stableId, ct);
        }

        lock (_lock)
        {
            if (TrySubscribeExisting(stableId) is { } raced)
            {
                return raced;
            }

            var session = new CaptureSession(source, factory, options.Value, loggerFactory.CreateLogger<CaptureSession>());
            session.Ended += OnSessionEnded;
            _sessions[stableId] = new Entry(session);
            session.Start();
            _logger.LogInformation("Started capture session for {Name} ({StableId})", source.Name, stableId);
            return new CaptureLease(session, session.Subscribe());
        }
    }

    /// <inheritdoc />
    public Task ReleaseAsync(CaptureLease lease)
    {
        var remaining = lease.Session.Unsubscribe(lease.Subscriber);
        if (remaining > 0)
        {
            return Task.CompletedTask;
        }

        lock (_lock)
        {
            if (_sessions.TryGetValue(lease.Session.Source.StableId, out var entry)
                && ReferenceEquals(entry.Session, lease.Session)
                && entry.IdleCts is null)
            {
                entry.IdleCts = new CancellationTokenSource();
                _ = StopWhenIdleAsync(entry, entry.IdleCts.Token);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task RestartAsync(string stableId, string reason)
    {
        Entry? entry;
        lock (_lock)
        {
            if (_sessions.Remove(stableId, out entry))
            {
                entry.IdleCts?.Cancel();
            }
        }

        if (entry is not null)
        {
            await entry.Session.StopAsync(reason);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<StreamSessionDto> GetSessions()
    {
        lock (_lock)
        {
            return _sessions.Values.Select(e => e.Session.GetSnapshot()).ToList();
        }
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        List<Entry> entries;
        lock (_lock)
        {
            entries = [.. _sessions.Values];
            _sessions.Clear();
        }

        foreach (var entry in entries)
        {
            entry.IdleCts?.Cancel();
        }

        await Task.WhenAll(entries.Select(e => e.Session.StopAsync("shutdown")));
    }

    private CaptureLease? TrySubscribeExisting(string stableId)
    {
        lock (_lock)
        {
            if (!_sessions.TryGetValue(stableId, out var entry) || entry.Session.IsEnded)
            {
                return null;
            }

            entry.IdleCts?.Cancel();
            entry.IdleCts = null;
            try
            {
                return new CaptureLease(entry.Session, entry.Session.Subscribe());
            }
            catch (InvalidOperationException)
            {
                // Ended between the check and Subscribe.
                return null;
            }
        }
    }

    private async Task StopWhenIdleAsync(Entry entry, CancellationToken ct)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(options.Value.IdleShutdownSeconds), timeProvider, ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        lock (_lock)
        {
            if (entry.Session.SubscriberCount > 0
                || !_sessions.TryGetValue(entry.Session.Source.StableId, out var current)
                || !ReferenceEquals(current, entry))
            {
                return;
            }

            _sessions.Remove(entry.Session.Source.StableId);
        }

        _logger.LogInformation("Stopping idle capture session for {Name}", entry.Session.Source.Name);
        await entry.Session.StopAsync("idle");
    }

    private void OnSessionEnded(CaptureSession session)
    {
        lock (_lock)
        {
            if (_sessions.TryGetValue(session.Source.StableId, out var entry) && ReferenceEquals(entry.Session, session))
            {
                _sessions.Remove(session.Source.StableId);
                entry.IdleCts?.Cancel();
            }
        }

        // Ensure ffmpeg is reaped when the pipeline ended on its own.
        _ = Task.Run(async () => await session.DisposeAsync());
    }

    private sealed class Entry(CaptureSession session)
    {
        public CaptureSession Session { get; } = session;

        public CancellationTokenSource? IdleCts { get; set; }
    }
}
