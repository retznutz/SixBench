namespace SixBench.Common.Dtos;

/// <summary>
/// A Roku device known to the server.
/// </summary>
/// <param name="Id">Database identifier.</param>
/// <param name="SerialNumber">Roku serial number (unique).</param>
/// <param name="FriendlyName">User-visible device name.</param>
/// <param name="Model">Model name.</param>
/// <param name="IpAddress">Last known IP address.</param>
/// <param name="Port">ECP port (normally 8060).</param>
/// <param name="IsManual">True if the device was added by IP rather than discovered.</param>
/// <param name="LastSeenUtc">When the device last responded.</param>
public sealed record RokuDeviceDto(
    int Id,
    string SerialNumber,
    string FriendlyName,
    string? Model,
    string IpAddress,
    int Port,
    bool IsManual,
    DateTime LastSeenUtc);
