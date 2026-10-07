namespace SixBench.Common.Options;

/// <summary>
/// ffmpeg settings, bound from the <c>Ffmpeg</c> configuration section.
/// </summary>
public sealed class FfmpegOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Ffmpeg";

    /// <summary>Path to the ffmpeg executable, or just <c>ffmpeg</c> to use PATH.</summary>
    public string Path { get; set; } = "ffmpeg";

    /// <summary>
    /// H.264 encoder: <c>libx264</c>, <c>h264_videotoolbox</c>, <c>h264_nvenc</c>, <c>h264_qsv</c>, <c>h264_amf</c>
    /// or <c>h264_vaapi</c> get tuned low-latency settings; any other encoder name is passed through as-is.
    /// </summary>
    public string VideoEncoder { get; set; } = "libx264";

    /// <summary>Extra encoder arguments appended after the defaults (space separated).</summary>
    public string? ExtraEncoderArgs { get; set; }

    /// <summary>Target video bitrate, e.g. <c>6M</c>. Null leaves the encoder default (CRF for libx264).</summary>
    public string? VideoBitrate { get; set; } = "6M";

    /// <summary>GOP length in seconds; also the worst-case wait for a keyframe.</summary>
    public double GopSeconds { get; set; } = 1.0;

    /// <summary>Default capture frame rate when an encoder link has no override.</summary>
    public double DefaultFrameRate { get; set; } = 30;

    /// <summary>Default capture size (e.g. <c>1920x1080</c>) when an encoder link has no override.</summary>
    public string? DefaultVideoSize { get; set; }

    /// <summary>Default capture pixel format when an encoder link has no override.</summary>
    public string? DefaultPixelFormat { get; set; }

    /// <summary>Timeout for device-listing commands, in milliseconds.</summary>
    public int ProbeTimeoutMs { get; set; } = 8000;
}
