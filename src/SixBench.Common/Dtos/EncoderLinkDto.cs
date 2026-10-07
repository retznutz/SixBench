namespace SixBench.Common.Dtos;

/// <summary>
/// The saved association between a capture encoder and the Roku plugged into it, plus capture settings.
/// </summary>
/// <param name="Id">Database identifier.</param>
/// <param name="CaptureDeviceStableId">Stable id of the capture device.</param>
/// <param name="DisplayName">Friendly name chosen by the user.</param>
/// <param name="VideoInput">ffmpeg video input specifier.</param>
/// <param name="AudioInput">ffmpeg audio input specifier (overrides automatic pairing).</param>
/// <param name="RokuDeviceId">The linked Roku, if any.</param>
/// <param name="RokuDevice">The linked Roku details, if any.</param>
/// <param name="AllowDeviceAudio">Whether device audio may be streamed to browsers.</param>
/// <param name="FrameRate">Capture frame rate override.</param>
/// <param name="VideoSize">Capture size override, e.g. <c>1920x1080</c>.</param>
/// <param name="PixelFormat">Capture pixel format override, e.g. <c>uyvy422</c>.</param>
/// <param name="CreatedUtc">When the link was created.</param>
/// <param name="UpdatedUtc">When the link was last modified.</param>
public sealed record EncoderLinkDto(
    int Id,
    string CaptureDeviceStableId,
    string DisplayName,
    string VideoInput,
    string? AudioInput,
    int? RokuDeviceId,
    RokuDeviceDto? RokuDevice,
    bool AllowDeviceAudio,
    double? FrameRate,
    string? VideoSize,
    string? PixelFormat,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);
