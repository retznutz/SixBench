using SixBench.Common.Enums;

namespace SixBench.Services.Capture;

/// <summary>
/// Lists the capture devices on the host for one platform backend.
/// </summary>
public interface ICaptureDeviceEnumerator
{
    /// <summary>The platform this enumerator supports.</summary>
    HostPlatform Platform { get; }

    /// <summary>
    /// Enumerates video and audio capture devices.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The detected devices.</returns>
    Task<DeviceInventory> EnumerateAsync(CancellationToken ct = default);
}
