using System.ComponentModel.DataAnnotations;

namespace SixBench.Data.Entities;

/// <summary>
/// Associates an HDMI capture encoder with the Roku plugged into it, plus per-device capture settings.
/// </summary>
public class EncoderLink
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Stable identifier of the capture device (unique).</summary>
    [Required, MaxLength(500)]
    public string CaptureDeviceStableId { get; set; } = string.Empty;

    /// <summary>Friendly name chosen by the user.</summary>
    [Required, MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>ffmpeg video input specifier.</summary>
    [Required, MaxLength(500)]
    public string VideoInput { get; set; } = string.Empty;

    /// <summary>ffmpeg audio input specifier; null uses automatic pairing.</summary>
    [MaxLength(500)]
    public string? AudioInput { get; set; }

    /// <summary>Foreign key to the linked Roku.</summary>
    public int? RokuDeviceId { get; set; }

    /// <summary>The linked Roku.</summary>
    public RokuDevice? RokuDevice { get; set; }

    /// <summary>Whether device audio may be streamed to browsers.</summary>
    public bool AllowDeviceAudio { get; set; }

    /// <summary>Capture frame rate override.</summary>
    public double? FrameRate { get; set; }

    /// <summary>Capture size override, e.g. <c>1920x1080</c>.</summary>
    [MaxLength(20)]
    public string? VideoSize { get; set; }

    /// <summary>Capture pixel format override.</summary>
    [MaxLength(32)]
    public string? PixelFormat { get; set; }

    /// <summary>When the link was created.</summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>When the link was last modified.</summary>
    public DateTime UpdatedUtc { get; set; }
}
