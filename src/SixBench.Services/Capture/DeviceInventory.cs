using SixBench.Common.Enums;

namespace SixBench.Services.Capture;

/// <summary>
/// Capture devices detected on the host at one point in time.
/// </summary>
/// <param name="Platform">Capture backend platform.</param>
/// <param name="Video">Video capture devices.</param>
/// <param name="Audio">Audio capture devices.</param>
public sealed record DeviceInventory(
    HostPlatform Platform,
    IReadOnlyList<VideoDeviceInfo> Video,
    IReadOnlyList<AudioDeviceInfo> Audio)
{
    /// <summary>An empty inventory.</summary>
    public static DeviceInventory Empty(HostPlatform platform) => new(platform, [], []);
}

/// <summary>
/// A video capture device.
/// </summary>
/// <param name="StableId">Identifier that survives re-plugging (platform prefixed).</param>
/// <param name="Name">Device name.</param>
/// <param name="Input">ffmpeg input specifier.</param>
public sealed record VideoDeviceInfo(string StableId, string Name, string Input);

/// <summary>
/// An audio capture device.
/// </summary>
/// <param name="Name">Device name.</param>
/// <param name="Input">ffmpeg input specifier.</param>
public sealed record AudioDeviceInfo(string Name, string Input);
