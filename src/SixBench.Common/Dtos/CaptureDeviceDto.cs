using SixBench.Common.Enums;

namespace SixBench.Common.Dtos;

/// <summary>
/// An HDMI-to-USB capture encoder detected on the host.
/// </summary>
/// <param name="Id">URL-safe key for this device (base64url of <paramref name="StableId"/>); use it in routes.</param>
/// <param name="StableId">An identifier that survives reboots and re-plugging (used as the key for links).</param>
/// <param name="Name">Human-readable device name reported by the OS.</param>
/// <param name="VideoInput">The ffmpeg input specifier for the video device.</param>
/// <param name="AudioInput">The ffmpeg input specifier for the paired audio device, if any.</param>
/// <param name="Platform">The capture backend platform.</param>
/// <param name="IsConnected">Whether the device is currently attached (false for saved links whose device is missing).</param>
/// <param name="Link">The saved encoder-to-Roku link, if one exists.</param>
public sealed record CaptureDeviceDto(
    string Id,
    string StableId,
    string Name,
    string VideoInput,
    string? AudioInput,
    HostPlatform Platform,
    bool IsConnected,
    EncoderLinkDto? Link);
