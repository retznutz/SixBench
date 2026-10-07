namespace SixBench.Common.Enums;

/// <summary>
/// The host operating system, which determines the capture backend used by ffmpeg.
/// </summary>
public enum HostPlatform
{
    /// <summary>Unsupported or unknown OS.</summary>
    Unknown,
    /// <summary>Windows (DirectShow).</summary>
    Windows,
    /// <summary>macOS (AVFoundation).</summary>
    MacOS,
    /// <summary>Linux (Video4Linux2 + ALSA).</summary>
    Linux,
}
