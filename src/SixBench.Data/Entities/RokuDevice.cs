using System.ComponentModel.DataAnnotations;

namespace SixBench.Data.Entities;

/// <summary>
/// A Roku device, either discovered via SSDP or added manually by IP.
/// </summary>
public class RokuDevice
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Roku serial number (unique; survives IP changes).</summary>
    [Required, MaxLength(64)]
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>User-visible device name.</summary>
    [Required, MaxLength(200)]
    public string FriendlyName { get; set; } = string.Empty;

    /// <summary>Model name.</summary>
    [MaxLength(100)]
    public string? Model { get; set; }

    /// <summary>Last known IP address or host name.</summary>
    [Required, MaxLength(255)]
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>ECP port.</summary>
    public int Port { get; set; } = 8060;

    /// <summary>True if added by IP rather than discovered.</summary>
    public bool IsManual { get; set; }

    /// <summary>When the device last responded.</summary>
    public DateTime LastSeenUtc { get; set; }

    /// <summary>
    /// Developer-mode web server password (user <c>rokudev</c>), encrypted with ASP.NET Core Data Protection.
    /// Null when not set.
    /// </summary>
    [MaxLength(2048)]
    public string? DevPasswordProtected { get; set; }

    /// <summary>Encoders linked to this Roku.</summary>
    public ICollection<EncoderLink> EncoderLinks { get; set; } = new List<EncoderLink>();
}
