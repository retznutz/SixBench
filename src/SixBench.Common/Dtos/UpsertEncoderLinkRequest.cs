using System.ComponentModel.DataAnnotations;

namespace SixBench.Common.Dtos;

/// <summary>
/// Request body to create or update an encoder link.
/// </summary>
public sealed class UpsertEncoderLinkRequest
{
    /// <summary>Friendly name for the encoder.</summary>
    [Required, StringLength(200, MinimumLength = 1)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>ffmpeg video input specifier (defaults to the detected device's value when omitted).</summary>
    [StringLength(500)]
    public string? VideoInput { get; set; }

    /// <summary>ffmpeg audio input specifier; null uses automatic pairing.</summary>
    [StringLength(500)]
    public string? AudioInput { get; set; }

    /// <summary>The Roku plugged into this encoder.</summary>
    public int? RokuDeviceId { get; set; }

    /// <summary>Whether device audio may be streamed to browsers.</summary>
    public bool AllowDeviceAudio { get; set; }

    /// <summary>Capture frame rate override.</summary>
    [Range(1, 240)]
    public double? FrameRate { get; set; }

    /// <summary>Capture size override, e.g. <c>1920x1080</c>.</summary>
    [RegularExpression(@"^\d{2,5}x\d{2,5}$")]
    public string? VideoSize { get; set; }

    /// <summary>Capture pixel format override, e.g. <c>uyvy422</c>.</summary>
    [RegularExpression(@"^[a-z0-9_]{2,32}$")]
    public string? PixelFormat { get; set; }
}
