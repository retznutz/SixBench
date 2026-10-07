using SixBench.Common.Enums;

namespace SixBench.Services.Capture;

/// <summary>
/// Enumerator for unsupported platforms; always returns an empty inventory.
/// </summary>
public sealed class UnsupportedDeviceEnumerator : ICaptureDeviceEnumerator
{
    /// <inheritdoc />
    public HostPlatform Platform => HostPlatform.Unknown;

    /// <inheritdoc />
    public Task<DeviceInventory> EnumerateAsync(CancellationToken ct = default) =>
        Task.FromResult(DeviceInventory.Empty(Platform));
}
