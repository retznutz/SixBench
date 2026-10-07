using SixBench.Common.Dtos;

namespace SixBench.Services.Roku;

/// <summary>
/// Manages the saved list of Roku devices.
/// </summary>
public interface IRokuDeviceService
{
    /// <summary>Lists saved devices.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All devices.</returns>
    Task<IReadOnlyList<RokuDeviceDto>> ListAsync(CancellationToken ct = default);

    /// <summary>Gets a saved device.</summary>
    /// <param name="id">Device id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The device.</returns>
    /// <exception cref="Exceptions.NotFoundException">No such device.</exception>
    Task<RokuDeviceDto> GetAsync(int id, CancellationToken ct = default);

    /// <summary>Discovers devices on the LAN and saves them (matched by serial number).</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The devices found by this discovery.</returns>
    Task<IReadOnlyList<RokuDeviceDto>> DiscoverAsync(CancellationToken ct = default);

    /// <summary>Adds a device by IP after verifying it responds to ECP.</summary>
    /// <param name="host">IP or host name.</param>
    /// <param name="port">ECP port, or null for the default.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The saved device.</returns>
    /// <exception cref="Exceptions.RokuUnreachableException">The device did not respond.</exception>
    Task<RokuDeviceDto> AddManualAsync(string host, int? port, CancellationToken ct = default);

    /// <summary>Deletes a saved device.</summary>
    /// <param name="id">Device id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if deleted.</returns>
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
