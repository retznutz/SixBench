using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixBench.Common.Dtos;
using SixBench.Common.Options;
using SixBench.Services.Exceptions;

namespace SixBench.Services.Certificates;

/// <summary>
/// Obtains Let's Encrypt certificates with the DNS-01 challenge, creating the TXT record through a DNS provider's API.
/// </summary>
/// <param name="providers">DNS providers.</param>
/// <param name="acme">Let's Encrypt client.</param>
/// <param name="dns">DNS lookups (to see the TXT record before Let's Encrypt is asked to check it).</param>
/// <param name="options">TLS options.</param>
/// <param name="logger">Logger.</param>
public sealed class CertificateService(
    DnsProviderFactory providers,
    IAcmeClient acme,
    IDnsTxtChecker dns,
    IOptions<TlsOptions> options,
    ILogger<CertificateService> logger)
{
    private static readonly TimeSpan PropagationPollInterval = TimeSpan.FromSeconds(5);

    // One certificate request at a time (setup wizard and renewal both write the same files and TXT record).
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>How long to wait when this server can't look the record up itself.</summary>
    public static TimeSpan BlindPropagationWait { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Creates the TXT record, waits for it to appear, has Let's Encrypt validate it, and saves the certificate as a PFX
    /// (no password, readable only by the server's user). The TXT record is removed afterwards, also on failure.
    /// </summary>
    /// <param name="domain">Domain.</param>
    /// <param name="email">Let's Encrypt account email.</param>
    /// <param name="dnsProviderName">DNS provider.</param>
    /// <param name="dnsCredentials">DNS provider credentials.</param>
    /// <param name="certOutputPath">Where to save the PFX.</param>
    /// <param name="acmeAccountPath">Where the ACME account key is kept.</param>
    /// <param name="progress">Progress reports.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><paramref name="certOutputPath"/>.</returns>
    /// <exception cref="CertificateRequestException">The DNS provider or Let's Encrypt failed.</exception>
    /// <exception cref="ServiceValidationException">Another certificate request is running.</exception>
    public async Task<string> ProvisionCertificateAsync(
        string domain,
        string email,
        string dnsProviderName,
        IReadOnlyDictionary<string, string> dnsCredentials,
        string certOutputPath,
        string acmeAccountPath,
        IProgress<CertificateProvisioningStatus> progress,
        CancellationToken ct = default)
    {
        var provider = providers.GetProvider(dnsProviderName);
        if (!await _gate.WaitAsync(0, ct))
        {
            throw new ServiceValidationException("A certificate request is already in progress.");
        }

        try
        {
            return await ProvisionCoreAsync(provider, domain, email, dnsCredentials, certOutputPath, acmeAccountPath, progress, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<string> ProvisionCoreAsync(
        IDnsChallengeProvider provider,
        string domain,
        string email,
        IReadOnlyDictionary<string, string> dnsCredentials,
        string certOutputPath,
        string acmeAccountPath,
        IProgress<CertificateProvisioningStatus> progress,
        CancellationToken ct)
    {
        var timeout = TimeSpan.FromSeconds(options.Value.ValidationTimeoutSeconds);

        progress.Report(new(CertificateProvisioningStep.CreatingOrder, "Creating ACME account and order..."));
        var order = await acme.CreateOrderAsync(domain, email, acmeAccountPath, options.Value.UseStaging, ct);

        var recordName = $"_acme-challenge.{domain}";
        if (order.RecordValue is { } recordValue)
        {
            progress.Report(new(CertificateProvisioningStep.SettingDnsRecord, $"Setting DNS TXT record at {recordName}..."));
            await CallProviderAsync(provider, () => provider.CreateTxtRecordAsync(domain, recordName, recordValue, dnsCredentials, ct));

            try
            {
                progress.Report(new(CertificateProvisioningStep.WaitingForPropagation, "Waiting for DNS propagation (this may take up to 2 minutes)..."));
                await WaitForDnsPropagationAsync(recordName, recordValue, timeout, ct);

                progress.Report(new(CertificateProvisioningStep.Validating, "Requesting Let's Encrypt to validate domain ownership..."));
                await order.ValidateAsync(timeout, ct);
            }
            finally
            {
                // Clean up even if the request was cancelled.
                try
                {
                    await provider.DeleteTxtRecordAsync(domain, recordName, recordValue, dnsCredentials, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to clean up DNS TXT record {RecordName}", recordName);
                }
            }
        }

        progress.Report(new(CertificateProvisioningStep.IssuingCertificate, "Generating certificate..."));
        var pfx = await order.IssuePfxAsync(ct);
        SecureFile.Write(certOutputPath, pfx);

        progress.Report(new(CertificateProvisioningStep.Complete, "Certificate provisioned successfully!"));
        logger.LogInformation("Certificate provisioned for {Domain}, saved to {Path}", domain, certOutputPath);
        return certOutputPath;
    }

    /// <summary>
    /// Points the domain's A record at an IP address.
    /// </summary>
    /// <param name="dnsProviderName">DNS provider.</param>
    /// <param name="domain">Domain.</param>
    /// <param name="ipAddress">IP address.</param>
    /// <param name="dnsCredentials">Credentials.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the record is saved.</returns>
    public async Task SetARecordAsync(string dnsProviderName, string domain, string ipAddress, IReadOnlyDictionary<string, string> dnsCredentials, CancellationToken ct = default)
    {
        var provider = providers.GetProvider(dnsProviderName);
        await CallProviderAsync(provider, () => provider.UpsertARecordAsync(domain, domain, ipAddress, dnsCredentials, ct));
    }

    /// <summary>
    /// Checks DNS provider credentials. Providers that need the domain to validate (GoDaddy) get it as <c>Domain</c>.
    /// </summary>
    /// <param name="dnsProviderName">DNS provider.</param>
    /// <param name="credentials">Credentials.</param>
    /// <param name="domain">Domain, if known.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that fails if the credentials don't work.</returns>
    public async Task ValidateProviderCredentialsAsync(string dnsProviderName, IReadOnlyDictionary<string, string> credentials, string? domain, CancellationToken ct = default)
    {
        var provider = providers.GetProvider(dnsProviderName);
        var missing = provider.RequiredCredentialKeys.Where(k => !credentials.TryGetValue(k, out var v) || string.IsNullOrWhiteSpace(v)).ToList();
        if (missing.Count > 0)
        {
            throw new ServiceValidationException($"Missing {string.Join(", ", missing)}.");
        }

        if (domain is not null && !credentials.ContainsKey("Domain"))
        {
            credentials = new Dictionary<string, string>(credentials) { ["Domain"] = domain };
        }

        await provider.ValidateCredentialsAsync(credentials, ct);
    }

    /// <summary>
    /// Reads the installed certificate.
    /// </summary>
    /// <param name="certPath">PFX path.</param>
    /// <returns>Its details, or null if there is no readable certificate.</returns>
    public static CertificateInfoDto? GetCurrentCertificateInfo(string certPath)
    {
        if (!File.Exists(certPath))
        {
            return null;
        }

        try
        {
            using var cert = X509CertificateLoader.LoadPkcs12FromFile(certPath, null);
            return new CertificateInfoDto(
                cert.GetNameInfo(X509NameType.SimpleName, false) ?? "Unknown",
                cert.NotBefore.ToUniversalTime(),
                cert.NotAfter.ToUniversalTime(),
                cert.Issuer,
                cert.Thumbprint);
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return null;
        }
    }

    private static async Task CallProviderAsync(IDnsChallengeProvider provider, Func<Task> call)
    {
        try
        {
            await call();
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or KeyNotFoundException)
        {
            throw new CertificateRequestException($"{provider.ProviderName} DNS update failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Polls the domain's nameservers until the TXT record is visible. If it never appears, Let's Encrypt is not asked
    /// (a failed validation counts against its rate limits). If this server can't do DNS lookups, waits a fixed time instead.
    /// </summary>
    private async Task WaitForDnsPropagationAsync(string recordName, string expectedValue, TimeSpan timeout, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (true)
        {
            try
            {
                var result = await dns.CheckAsync(recordName, expectedValue, ct);
                if (result.Found)
                {
                    return;
                }

                if (DateTime.UtcNow + PropagationPollInterval > deadline)
                {
                    throw new CertificateRequestException(
                        $"The TXT record {recordName} did not appear on {result.Source} within {timeout.TotalSeconds:0} seconds, "
                        + "so Let's Encrypt was not asked to validate it. Check the DNS provider and try again.");
                }
            }
            catch (Exception ex) when (ex is not CertificateRequestException and not OperationCanceledException)
            {
                logger.LogWarning(ex, "Could not look up {RecordName}; waiting {Seconds}s instead", recordName, BlindPropagationWait.TotalSeconds);
                await Task.Delay(BlindPropagationWait, ct);
                return;
            }

            await Task.Delay(PropagationPollInterval, ct);
        }
    }
}
