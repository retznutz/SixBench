namespace SixBench.Services.Certificates;

/// <summary>
/// Steps of a certificate request, reported as progress.
/// </summary>
public enum CertificateProvisioningStep
{
    /// <summary>Pointing the domain's A record at the server.</summary>
    SettingARecord,

    /// <summary>Creating the ACME account and order.</summary>
    CreatingOrder,

    /// <summary>Creating the <c>_acme-challenge</c> TXT record.</summary>
    SettingDnsRecord,

    /// <summary>Waiting for the TXT record to appear on the domain's nameservers.</summary>
    WaitingForPropagation,

    /// <summary>Let's Encrypt is checking the TXT record.</summary>
    Validating,

    /// <summary>Generating and downloading the certificate.</summary>
    IssuingCertificate,

    /// <summary>Done.</summary>
    Complete,

    /// <summary>The request failed.</summary>
    Failed,
}

/// <summary>
/// A progress report.
/// </summary>
/// <param name="Step">The step.</param>
/// <param name="Message">What is happening.</param>
public sealed record CertificateProvisioningStatus(CertificateProvisioningStep Step, string Message);

/// <summary>
/// Known DNS provider names.
/// </summary>
public static class DnsProviders
{
    /// <summary>Cloudflare.</summary>
    public const string Cloudflare = "Cloudflare";

    /// <summary>DuckDNS.</summary>
    public const string DuckDns = "DuckDNS";

    /// <summary>Amazon Route 53.</summary>
    public const string Route53 = "Route53";

    /// <summary>DigitalOcean.</summary>
    public const string DigitalOcean = "DigitalOcean";

    /// <summary>GoDaddy.</summary>
    public const string GoDaddy = "GoDaddy";

    /// <summary>All supported providers.</summary>
    public static readonly IReadOnlyList<string> All = [Cloudflare, DuckDns, Route53, DigitalOcean, GoDaddy];
}
