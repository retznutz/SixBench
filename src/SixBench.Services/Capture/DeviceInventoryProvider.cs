namespace SixBench.Services.Capture;

/// <summary>
/// Caches the device inventory briefly; enumeration spawns processes and takes hundreds of milliseconds.
/// </summary>
public interface IDeviceInventoryProvider
{
    /// <summary>
    /// Returns the current inventory, re-enumerating when the cache is stale or <paramref name="refresh"/> is true.
    /// </summary>
    /// <param name="refresh">Bypass the cache.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The inventory.</returns>
    Task<DeviceInventory> GetAsync(bool refresh = false, CancellationToken ct = default);
}

/// <summary>
/// Default <see cref="IDeviceInventoryProvider"/> with a short time-based cache.
/// </summary>
/// <param name="enumerator">The platform enumerator.</param>
/// <param name="timeProvider">Clock.</param>
public sealed class DeviceInventoryProvider(ICaptureDeviceEnumerator enumerator, TimeProvider timeProvider)
    : IDeviceInventoryProvider, IDisposable
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(5);

    private readonly SemaphoreSlim _lock = new(1, 1);
    private DeviceInventory? _cached;
    private DateTimeOffset _cachedAt;

    /// <inheritdoc />
    public async Task<DeviceInventory> GetAsync(bool refresh = false, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var now = timeProvider.GetUtcNow();
            if (!refresh && _cached is not null && now - _cachedAt < CacheDuration)
            {
                return _cached;
            }

            _cached = await enumerator.EnumerateAsync(ct);
            _cachedAt = now;
            return _cached;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _lock.Dispose();
}
