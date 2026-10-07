using System.ComponentModel.DataAnnotations;

namespace SixBench.Common.Dtos;

/// <summary>
/// The installed certificate.
/// </summary>
/// <param name="Domain">Subject name.</param>
/// <param name="NotBefore">Start of validity (UTC).</param>
/// <param name="NotAfter">End of validity (UTC).</param>
/// <param name="Issuer">Issuer distinguished name.</param>
/// <param name="Thumbprint">SHA-1 thumbprint.</param>
public sealed record CertificateInfoDto(string Domain, DateTime NotBefore, DateTime NotAfter, string Issuer, string Thumbprint);

/// <summary>
/// HTTPS certificate state.
/// </summary>
/// <param name="HasCertificate">A certificate file is installed.</param>
/// <param name="Info">Details of the installed certificate.</param>
/// <param name="AutoRenewEnabled">TLS is enabled and encrypted DNS credentials are stored for renewal.</param>
/// <param name="HttpsActive">The server is currently serving HTTPS (false until restarted after the first certificate).</param>
/// <param name="HttpsUrl">Where the server is reachable over HTTPS.</param>
/// <param name="Port">The server's port (<c>Server:Port</c>), for port-forwarding instructions.</param>
public sealed record CertificateStatusDto(bool HasCertificate, CertificateInfoDto? Info, bool AutoRenewEnabled, bool HttpsActive, string? HttpsUrl, int Port);

/// <summary>
/// A supported DNS provider.
/// </summary>
/// <param name="Name">Provider name.</param>
/// <param name="RequiredCredentials">Credential keys the provider needs.</param>
public sealed record DnsProviderInfoDto(string Name, IReadOnlyList<string> RequiredCredentials);

/// <summary>
/// Result of checking DNS provider credentials.
/// </summary>
/// <param name="Success">True if the credentials work.</param>
/// <param name="Message">What happened.</param>
public sealed record CredentialValidationResultDto(bool Success, string Message);

/// <summary>
/// The server's public IP address.
/// </summary>
/// <param name="Ip">IPv4 or IPv6 address.</param>
public sealed record PublicIpDto(string Ip);

/// <summary>
/// Progress of a certificate request, pushed over the certificate hub.
/// </summary>
/// <param name="Step">Step name (see <c>CertificateProvisioningStep</c>).</param>
/// <param name="Message">What is happening.</param>
public sealed record CertificateProgressDto(string Step, string Message);

/// <summary>
/// Request body to test DNS provider credentials.
/// </summary>
public sealed class ValidateCredentialsRequest
{
    /// <summary>DNS provider name.</summary>
    [Required]
    public string DnsProvider { get; set; } = string.Empty;

    /// <summary>Credential key → value.</summary>
    [Required]
    public Dictionary<string, string> DnsCredentials { get; set; } = new();

    /// <summary>Domain (some providers need it to validate).</summary>
    public string? Domain { get; set; }
}

/// <summary>
/// Request body to obtain and install a certificate.
/// </summary>
public sealed class ProvisionCertificateRequest
{
    /// <summary>Full domain name, e.g. <c>sixbench.example.com</c>.</summary>
    [Required, StringLength(253, MinimumLength = 3)]
    [RegularExpression(
        @"^([A-Za-z0-9]([A-Za-z0-9-]{0,61}[A-Za-z0-9])?\.)+[A-Za-z]{2,63}$",
        ErrorMessage = "Enter a domain name such as sixbench.example.com.")]
    public string Domain { get; set; } = string.Empty;

    /// <summary>Let's Encrypt account email.</summary>
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    /// <summary>DNS provider name.</summary>
    [Required]
    public string DnsProvider { get; set; } = string.Empty;

    /// <summary>Credential key → value. Stored encrypted for automatic renewal.</summary>
    [Required]
    public Dictionary<string, string> DnsCredentials { get; set; } = new();

    /// <summary>Also point the domain's A record at <see cref="PublicIp"/>.</summary>
    public bool SetupDnsRecord { get; set; }

    /// <summary>IP address for the A record.</summary>
    public string? PublicIp { get; set; }
}
