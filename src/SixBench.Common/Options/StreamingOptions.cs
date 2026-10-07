namespace SixBench.Common.Options;

/// <summary>
/// Stream fan-out settings, bound from the <c>Streaming</c> configuration section.
/// </summary>
public sealed class StreamingOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Streaming";

    /// <summary>Seconds to keep ffmpeg running after the last client disconnects.</summary>
    public int IdleShutdownSeconds { get; set; } = 10;

    /// <summary>Per-client queue length in messages before the server drops to the next keyframe.</summary>
    public int SubscriberQueueFrames { get; set; } = 90;

    /// <summary>Maximum size of an inbound control message in bytes.</summary>
    public int MaxControlMessageBytes { get; set; } = 16 * 1024;
}
