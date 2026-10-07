namespace SixBench.Common.Options;

/// <summary>
/// Audio policy and encoding settings, bound from the <c>Audio</c> configuration section.
/// </summary>
public sealed class AudioOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Audio";

    /// <summary>Global switch for device audio; must be true and the encoder link must also allow it.</summary>
    public bool AllowDeviceAudio { get; set; } = true;

    /// <summary>Opus bitrate for device audio, e.g. <c>128k</c>.</summary>
    public string DeviceBitrate { get; set; } = "128k";
}
