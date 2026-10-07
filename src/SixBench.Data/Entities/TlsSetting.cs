using System.ComponentModel.DataAnnotations;

namespace SixBench.Data.Entities;

/// <summary>
/// HTTPS settings saved from the web app. There is at most one row; it overrides the <c>Tls</c> configuration section.
/// </summary>
public class TlsSetting
{
    /// <summary>The id of the only row.</summary>
    public const int SingletonId = 1;

    /// <summary>Primary key (always <see cref="SingletonId"/>).</summary>
    public int Id { get; set; }

    /// <summary>Serve HTTPS when the certificate file exists (read at startup).</summary>
    public bool Enabled { get; set; }

    /// <summary>Domain the certificate is for.</summary>
    [MaxLength(253)]
    public string? Domain { get; set; }

    /// <summary>Let's Encrypt account email.</summary>
    [MaxLength(254)]
    public string? Email { get; set; }

    /// <summary>DNS provider name.</summary>
    [MaxLength(50)]
    public string? DnsProvider { get; set; }

    /// <summary>DNS provider credentials as JSON, each value encrypted with ASP.NET Core Data Protection.</summary>
    public string? EncryptedDnsCredentials { get; set; }

    /// <summary>When the row was last saved.</summary>
    public DateTime UpdatedUtc { get; set; }
}
