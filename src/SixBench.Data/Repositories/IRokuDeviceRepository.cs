using SixBench.Data.Entities;

namespace SixBench.Data.Repositories;

/// <summary>
/// Persistence for <see cref="RokuDevice"/>.
/// </summary>
public interface IRokuDeviceRepository
{
    /// <summary>Lists all devices ordered by name.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All known devices.</returns>
    Task<IReadOnlyList<RokuDevice>> ListAsync(CancellationToken ct = default);

    /// <summary>Gets a device by id.</summary>
    /// <param name="id">Device id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The device, or null.</returns>
    Task<RokuDevice?> GetAsync(int id, CancellationToken ct = default);

    /// <summary>Gets a device by serial number.</summary>
    /// <param name="serialNumber">Roku serial number.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The device, or null.</returns>
    Task<RokuDevice?> GetBySerialAsync(string serialNumber, CancellationToken ct = default);

    /// <summary>
    /// Inserts a new device or updates the existing one with the same serial number.
    /// <see cref="RokuDevice.IsManual"/> is only ever cleared on insert, never by a later discovery.
    /// </summary>
    /// <param name="device">Device values.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The saved device.</returns>
    Task<RokuDevice> UpsertBySerialAsync(RokuDevice device, CancellationToken ct = default);

    /// <summary>Deletes a device; links to it are set to null.</summary>
    /// <param name="id">Device id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if a device was deleted.</returns>
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
