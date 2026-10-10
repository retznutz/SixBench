namespace SixBench.Common.Options;

/// <summary>
/// HTTPS settings, bound from the <c>Tls</c> configuration section. Values saved from the web app
/// (the <c>TlsSetting</c> table) take precedence.
/// </summary>
public sealed class TlsOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Tls";

    /// <summary>Serve HTTPS on <see cref="ServerOptions.Port"/> when the certificate file exists.</summary>
    public bool Enabled { get; set; }

    /// <summary>Domain the certificate is for.</summary>
    public string? Domain { get; set; }

    /// <summary>Let's Encrypt account email.</summary>
    public string? Email { get; set; }

    /// <summary>DNS provider used for the DNS-01 challenge (see <c>DnsProviders</c>).</summary>
    public string? DnsProvider { get; set; }

    /// <summary>Plain-text DNS provider credentials (legacy; the web app stores them encrypted instead).</summary>
    public Dictionary<string, string> DnsCredentials { get; set; } = new();

    /// <summary>Where the certificate and ACME account key are stored. Relative paths resolve against the app folder.</summary>
    public string CertificateDirectory { get; set; } = "data/certs";

    /// <summary>Use the Let's Encrypt staging server (untrusted test certificates, generous rate limits).</summary>
    public bool UseStaging { get; set; }

    /// <summary>How long to wait for the TXT record to appear and for Let's Encrypt to validate it, in seconds.</summary>
    public int ValidationTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Extra wait after the domain's nameservers all serve the TXT record, before Let's Encrypt is asked, in seconds.
    /// Providers like GoDaddy answer from many anycast servers that update a little behind the ones this server sees.
    /// </summary>
    public int DnsSettleSeconds { get; set; } = 30;
}
