using SixBench.Common.Enums;

namespace SixBench.Services.Capture;

/// <summary>
/// Everything needed to start capturing from one encoder: inputs, capture settings and the audio grant.
/// </summary>
/// <param name="StableId">Capture device stable id.</param>
/// <param name="Name">Display name.</param>
/// <param name="Platform">Capture backend platform.</param>
/// <param name="VideoInput">ffmpeg video input specifier.</param>
/// <param name="AudioInput">ffmpeg audio input specifier, if any.</param>
/// <param name="FrameRate">Capture frame rate.</param>
/// <param name="VideoSize">Capture size, e.g. <c>1920x1080</c>, or null for the device default.</param>
/// <param name="PixelFormat">Capture pixel format, or null for the device default.</param>
/// <param name="AudioGrant">What audio the server permits for this encoder.</param>
public sealed record CaptureSource(
    string StableId,
    string Name,
    HostPlatform Platform,
    string VideoInput,
    string? AudioInput,
    double FrameRate,
    string? VideoSize,
    string? PixelFormat,
    AudioGrant AudioGrant);

/// <summary>
/// The server's audio permission for a session (sent to clients as <c>audio_grant</c>).
/// </summary>
/// <param name="Device">Device audio may be streamed to the client.</param>
/// <param name="Mic">The client's microphone may be sent to the device (always false in v1).</param>
public sealed record AudioGrant(bool Device, bool Mic);
