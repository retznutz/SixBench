using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SixBench.Common.Dtos;
using SixBench.Common.Options;
using SixBench.Common.Streaming;
using SixBench.Common.Utilities;
using SixBench.Services.Capture;

namespace SixBench.Services.Streaming;

/// <summary>
/// Result of toggling device audio for a subscriber.
/// </summary>
public enum AudioToggleResult
{
    /// <summary>The change was applied.</summary>
    Ok,
    /// <summary>Device audio is not permitted for this encoder.</summary>
    NotPermitted,
    /// <summary>The session has ended.</summary>
    Ended,
}

/// <summary>
/// One capture device being streamed: a single ffmpeg video pipeline (and an on-demand audio pipeline)
/// fanned out to any number of <see cref="StreamSubscriber"/>s.
/// </summary>
/// <remarks>
/// USB capture devices only allow one reader, so there is exactly one session per device.
/// The session caches every access unit since the most recent keyframe (the current GOP), so new
/// clients and clients recovering from a decode error can start decoding immediately.
/// </remarks>
public sealed class CaptureSession : IAsyncDisposable
{
    private const int MaxGopFrames = 600;

    private readonly IMediaSourceFactory _factory;
    private readonly StreamingOptions _options;
    private readonly ILogger _logger;
    private readonly object _gate = new();
    private readonly List<StreamSubscriber> _subscribers = [];
    private readonly List<AccessUnit> _gop = [];
    private readonly CancellationTokenSource _cts = new();
    private readonly Stopwatch _clock = new();

    private IVideoSource? _video;
    private Task? _videoPump;
    private IAudioSource? _audio;
    private CancellationTokenSource? _audioCts;
    private Task? _audioPump;
    private int _audioGeneration;
    private uint _audioSequence;
    private long _framesOut;
    private long _bytesOut;
    private bool _ended;
    private int _disposed;

    /// <summary>
    /// Creates a session; call <see cref="Start"/> to begin capture.
    /// </summary>
    /// <param name="source">What to capture.</param>
    /// <param name="factory">Media source factory.</param>
    /// <param name="options">Streaming options.</param>
    /// <param name="logger">Logger.</param>
    public CaptureSession(CaptureSource source, IMediaSourceFactory factory, StreamingOptions options, ILogger logger)
    {
        Source = source;
        _factory = factory;
        _options = options;
        _logger = logger;
    }

    /// <summary>Raised once when the session ends for any reason.</summary>
    public event Action<CaptureSession>? Ended;

    /// <summary>What is being captured.</summary>
    public CaptureSource Source { get; }

    /// <summary>When capture started.</summary>
    public DateTime StartedUtc { get; private set; }

    /// <summary>Whether the session has ended.</summary>
    public bool IsEnded
    {
        get
        {
            lock (_gate)
            {
                return _ended;
            }
        }
    }

    /// <summary>Number of connected subscribers.</summary>
    public int SubscriberCount
    {
        get
        {
            lock (_gate)
            {
                return _subscribers.Count;
            }
        }
    }

    /// <summary>
    /// Starts the video pipeline.
    /// </summary>
    public void Start()
    {
        StartedUtc = DateTime.UtcNow;
        _clock.Start();
        _video = _factory.CreateVideo(Source);
        _videoPump = Task.Run(() => VideoPumpAsync(_video, _cts.Token));
    }

    /// <summary>
    /// Adds a subscriber and queues the cached GOP for it.
    /// </summary>
    /// <returns>The new subscriber.</returns>
    /// <exception cref="InvalidOperationException">The session has ended.</exception>
    public StreamSubscriber Subscribe()
    {
        lock (_gate)
        {
            if (_ended)
            {
                throw new InvalidOperationException("The capture session has ended.");
            }

            var id = Guid.NewGuid().ToString("N")[..12];
            var subscriber = new StreamSubscriber(id, _options.SubscriberQueueFrames);
            _subscribers.Add(subscriber);
            subscriber.ReplayGop(_gop);
            return subscriber;
        }
    }

    /// <summary>
    /// Removes a subscriber.
    /// </summary>
    /// <param name="subscriber">The subscriber.</param>
    /// <returns>The number of remaining subscribers.</returns>
    public int Unsubscribe(StreamSubscriber subscriber)
    {
        int remaining;
        lock (_gate)
        {
            _subscribers.Remove(subscriber);
            subscriber.AudioEnabled = false;
            remaining = _subscribers.Count;
        }

        subscriber.Complete<object>(null);
        UpdateAudioPipeline();
        return remaining;
    }

    /// <summary>
    /// Resends the cached GOP to a subscriber (after a client decode error).
    /// </summary>
    /// <param name="subscriber">The subscriber.</param>
    public void RequestKeyframe(StreamSubscriber subscriber)
    {
        lock (_gate)
        {
            subscriber.ReplayGop(_gop);
        }
    }

    /// <summary>
    /// Turns device audio on or off for a subscriber, starting or stopping the audio pipeline as needed.
    /// </summary>
    /// <param name="subscriber">The subscriber.</param>
    /// <param name="enabled">Desired state.</param>
    /// <returns>The outcome.</returns>
    public AudioToggleResult SetAudio(StreamSubscriber subscriber, bool enabled)
    {
        if (enabled && !Source.AudioGrant.Device)
        {
            return AudioToggleResult.NotPermitted;
        }

        lock (_gate)
        {
            if (_ended)
            {
                return AudioToggleResult.Ended;
            }

            subscriber.AudioEnabled = enabled;
        }

        UpdateAudioPipeline();
        return AudioToggleResult.Ok;
    }

    /// <summary>
    /// Returns diagnostics for this session.
    /// </summary>
    /// <returns>The session snapshot.</returns>
    public StreamSessionDto GetSnapshot()
    {
        lock (_gate)
        {
            return new StreamSessionDto(
                DeviceKey.Encode(Source.StableId),
                Source.StableId,
                StartedUtc,
                _subscribers.Count,
                _subscribers.Count(s => s.AudioEnabled),
                Interlocked.Read(ref _framesOut),
                Interlocked.Read(ref _bytesOut),
                _audioPump is not null,
                _subscribers
                    .Select(s => s.LastStats is { } stats
                        ? stats with { ServerDroppedFrames = s.ServerDroppedFrames }
                        : new ClientStatsDto(s.Id, null, null, null, null, s.ServerDroppedFrames, null))
                    .ToList());
        }
    }

    /// <summary>
    /// Ends the session, notifying subscribers with <paramref name="reason"/>, and stops capture.
    /// </summary>
    /// <param name="reason">Reason sent in <c>stream_ended</c>.</param>
    /// <param name="message">Optional detail.</param>
    /// <returns>A task that completes when capture has stopped.</returns>
    public async Task StopAsync(string reason, string? message = null)
    {
        End(reason, message);
        await DisposeAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        End("shutdown", null);
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        await _cts.CancelAsync();
        await StopAudioAsync();

        if (_videoPump is not null)
        {
            await Task.WhenAny(_videoPump, Task.Delay(TimeSpan.FromSeconds(5)));
        }

        if (_video is not null)
        {
            await _video.DisposeAsync();
        }

        _cts.Dispose();
    }

    private async Task VideoPumpAsync(IVideoSource video, CancellationToken ct)
    {
        string? failure = null;
        try
        {
            await foreach (var unit in video.ReadAsync(ct))
            {
                Interlocked.Increment(ref _framesOut);
                Interlocked.Add(ref _bytesOut, unit.Data.Length);
                lock (_gate)
                {
                    if (unit.IsKeyframe)
                    {
                        _gop.Clear();
                    }

                    if (_gop.Count < MaxGopFrames)
                    {
                        _gop.Add(unit);
                    }

                    foreach (var subscriber in _subscribers)
                    {
                        subscriber.OfferVideo(unit);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Video pipeline for {Name} failed", Source.Name);
            failure = ex.Message;
        }

        if (!ct.IsCancellationRequested)
        {
            End(StreamProtocol.ErrorCodes.CaptureFailed, video.FailureReason ?? failure ?? "The capture pipeline ended.");
        }
    }

    private void UpdateAudioPipeline()
    {
        IAudioSource? toStop = null;
        Task? stopTask = null;
        CancellationTokenSource? stopCts = null;

        lock (_gate)
        {
            var wanted = !_ended && Source.AudioGrant.Device && _subscribers.Any(s => s.AudioEnabled);
            if (wanted && _audioPump is null)
            {
                var generation = ++_audioGeneration;
                _audioCts = new CancellationTokenSource();
                _audio = _factory.CreateAudio(Source);
                var audio = _audio;
                var token = _audioCts.Token;
                _audioPump = Task.Run(() => AudioPumpAsync(audio, generation, token));
            }
            else if (!wanted && _audioPump is not null)
            {
                (toStop, stopTask, stopCts) = (_audio, _audioPump, _audioCts);
                _audio = null;
                _audioPump = null;
                _audioCts = null;
            }
        }

        if (toStop is not null)
        {
            _ = StopAudioSourceAsync(toStop, stopTask, stopCts);
        }
    }

    private async Task StopAudioAsync()
    {
        IAudioSource? audio;
        Task? pump;
        CancellationTokenSource? cts;
        lock (_gate)
        {
            (audio, pump, cts) = (_audio, _audioPump, _audioCts);
            _audio = null;
            _audioPump = null;
            _audioCts = null;
        }

        if (audio is not null)
        {
            await StopAudioSourceAsync(audio, pump, cts);
        }
    }

    private async Task StopAudioSourceAsync(IAudioSource audio, Task? pump, CancellationTokenSource? cts)
    {
        try
        {
            if (cts is not null)
            {
                await cts.CancelAsync();
            }

            if (pump is not null)
            {
                await Task.WhenAny(pump, Task.Delay(TimeSpan.FromSeconds(5)));
            }

            await audio.DisposeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error stopping audio for {Name}", Source.Name);
        }
        finally
        {
            cts?.Dispose();
        }
    }

    private async Task AudioPumpAsync(IAudioSource audio, int generation, CancellationToken ct)
    {
        var first = true;
        string? failure = null;
        try
        {
            await foreach (var packet in audio.ReadAsync(ct))
            {
                var header = new AudioFrameHeader(
                    first ? AudioFrameFlags.Discontinuity : AudioFrameFlags.None,
                    unchecked(_audioSequence++),
                    unchecked((uint)_clock.ElapsedMilliseconds));
                first = false;
                var frame = header.Frame(packet);
                lock (_gate)
                {
                    foreach (var subscriber in _subscribers)
                    {
                        subscriber.OfferAudio(frame);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audio pipeline for {Name} failed", Source.Name);
            failure = ex.Message;
        }

        if (ct.IsCancellationRequested)
        {
            return;
        }

        // Audio died on its own: tell listening clients and let video continue.
        var message = audio.FailureReason ?? failure ?? "The audio pipeline ended.";
        lock (_gate)
        {
            if (generation != _audioGeneration || _audioPump is null)
            {
                return;
            }

            foreach (var subscriber in _subscribers.Where(s => s.AudioEnabled))
            {
                subscriber.AudioEnabled = false;
                subscriber.SendControl(new AudioStateMessage(false, StreamProtocol.ErrorCodes.AudioUnavailable, message));
            }

            _audio = null;
            _audioPump = null;
            _audioCts?.Dispose();
            _audioCts = null;
        }

        await audio.DisposeAsync();
    }

    private void End(string reason, string? message)
    {
        List<StreamSubscriber> subscribers;
        lock (_gate)
        {
            if (_ended)
            {
                return;
            }

            _ended = true;
            subscribers = [.. _subscribers];
            _subscribers.Clear();
            _gop.Clear();
        }

        _logger.LogInformation("Capture session {Name} ended: {Reason} {Message}", Source.Name, reason, message);
        foreach (var subscriber in subscribers)
        {
            subscriber.Complete(new StreamEndedMessage(reason, message));
        }

        Ended?.Invoke(this);
    }
}
