using System.Text.Json;
using System.Threading.Channels;
using SixBench.Common.Dtos;

namespace SixBench.Services.Streaming;

/// <summary>
/// One connected browser's view of a capture session: a bounded media queue and an unbounded control queue.
/// </summary>
/// <remarks>
/// When the media queue overflows (slow client), the queued frames are discarded and the subscriber
/// waits for the next keyframe. Dropping individual delta frames would corrupt the decoder.
/// All Offer/Replay methods are called by <see cref="CaptureSession"/> while it holds its lock.
/// </remarks>
public sealed class StreamSubscriber
{
    private readonly Channel<byte[]> _media;
    private readonly Channel<string> _control = Channel.CreateUnbounded<string>(
        new UnboundedChannelOptions { SingleReader = true });

    private bool _waitingForKeyframe = true;
    private long _serverDropped;
    private volatile bool _audioEnabled;

    /// <summary>
    /// Creates a subscriber.
    /// </summary>
    /// <param name="id">Client id.</param>
    /// <param name="queueCapacity">Maximum queued media messages.</param>
    public StreamSubscriber(string id, int queueCapacity)
    {
        Id = id;
        _media = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(Math.Max(8, queueCapacity))
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false,
        });
    }

    /// <summary>Client id.</summary>
    public string Id { get; }

    /// <summary>Video and audio messages to send, in order.</summary>
    public ChannelReader<byte[]> Media => _media.Reader;

    /// <summary>JSON control messages to send.</summary>
    public ChannelReader<string> Control => _control.Reader;

    /// <summary>Whether this client receives device audio.</summary>
    public bool AudioEnabled
    {
        get => _audioEnabled;
        internal set => _audioEnabled = value;
    }

    /// <summary>Frames dropped by the server for this client.</summary>
    public long ServerDroppedFrames => Interlocked.Read(ref _serverDropped);

    /// <summary>The most recent stats reported by the client.</summary>
    public ClientStatsDto? LastStats { get; private set; }

    /// <summary>
    /// Queues a JSON control message.
    /// </summary>
    /// <typeparam name="T">Message type.</typeparam>
    /// <param name="message">The message.</param>
    public void SendControl<T>(T message) =>
        _control.Writer.TryWrite(JsonSerializer.Serialize(message, StreamJson.Options));

    /// <summary>
    /// Records stats reported by the client.
    /// </summary>
    /// <param name="decoder">Decoder name.</param>
    /// <param name="fps">Rendered fps.</param>
    /// <param name="jitterMs">Arrival jitter.</param>
    /// <param name="droppedFrames">Client-side drops.</param>
    public void UpdateStats(string? decoder, double? fps, double? jitterMs, long? droppedFrames) =>
        LastStats = new ClientStatsDto(Id, decoder, fps, jitterMs, droppedFrames, ServerDroppedFrames, DateTime.UtcNow);

    internal void OfferVideo(AccessUnit unit)
    {
        if (_waitingForKeyframe)
        {
            if (!unit.IsKeyframe)
            {
                Interlocked.Increment(ref _serverDropped);
                return;
            }

            _waitingForKeyframe = false;
        }

        if (_media.Writer.TryWrite(unit.Data))
        {
            return;
        }

        // Too far behind: discard everything queued and resume at the next keyframe.
        DrainMedia();
        Interlocked.Increment(ref _serverDropped);
        _waitingForKeyframe = true;
        if (unit.IsKeyframe && _media.Writer.TryWrite(unit.Data))
        {
            _waitingForKeyframe = false;
        }
    }

    internal void OfferAudio(byte[] frame)
    {
        if (_audioEnabled && !_waitingForKeyframe)
        {
            _media.Writer.TryWrite(frame);
        }
    }

    internal void ReplayGop(IReadOnlyList<AccessUnit> gop)
    {
        DrainMedia();
        _waitingForKeyframe = true;
        foreach (var unit in gop)
        {
            OfferVideo(unit);
        }
    }

    internal void Complete<T>(T? finalMessage)
        where T : class
    {
        if (finalMessage is not null)
        {
            SendControl(finalMessage);
        }

        _media.Writer.TryComplete();
        _control.Writer.TryComplete();
    }

    private void DrainMedia()
    {
        while (_media.Reader.TryRead(out _))
        {
        }
    }
}
