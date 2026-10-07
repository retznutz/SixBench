using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixBench.Common.Options;

namespace SixBench.Services.Certificates;

/// <summary>
/// Renews the certificate daily once it is within 30 days of expiry, using the stored DNS credentials,
/// and loads the new certificate without a restart.
/// </summary>
/// <param name="certificates">Certificate service.</param>
/// <param name="serverCertificate">The certificate Kestrel serves.</param>
/// <param name="scopes">Scope factory (settings are scoped).</param>
/// <param name="protector">Credential decryption.</param>
/// <param name="options">TLS options.</param>
/// <param name="environment">Host environment.</param>
/// <param name="time">Clock.</param>
/// <param name="logger">Logger.</param>
public sealed class CertificateRenewalBackgroundService(
    CertificateService certificates,
    IServerCertificate serverCertificate,
    IServiceScopeFactory scopes,
    IDnsCredentialProtector protector,
    IOptions<TlsOptions> options,
    IHostEnvironment environment,
    TimeProvider time,
    ILogger<CertificateRenewalBackgroundService> logger) : BackgroundService
{
    /// <summary>Renew when the certificate expires within this many days.</summary>
    public const int RenewWithinDays = 30;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunRenewalCheckAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(24), time);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunRenewalCheckAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    /// <summary>
    /// Renews the certificate if it is due.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if a certificate was renewed.</returns>
    public async Task<bool> CheckAndRenewAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var tls = await scope.ServiceProvider.GetRequiredService<ITlsSettingsService>().GetAsync(ct);
        if (!tls.Enabled || string.IsNullOrWhiteSpace(tls.Domain) || string.IsNullOrWhiteSpace(tls.Email) || string.IsNullOrWhiteSpace(tls.DnsProvider))
        {
            logger.LogDebug("Certificate renewal disabled or missing TLS renewal settings");
            return false;
        }

        IReadOnlyDictionary<string, string> credentials;
        if (tls.EncryptedDnsCredentials.Count > 0)
        {
            credentials = protector.Unprotect(tls.EncryptedDnsCredentials);
        }
        else if (tls.LegacyDnsCredentials.Count > 0)
        {
            logger.LogWarning("Using plain-text DNS credentials from Tls:DnsCredentials for renewal. Run the HTTPS setup in Settings to store them encrypted.");
            credentials = tls.LegacyDnsCredentials;
        }
        else
        {
            logger.LogWarning("Certificate renewal skipped because no DNS credentials are stored");
            return false;
        }

        var certPath = CertificatePaths.Certificate(options.Value, environment.ContentRootPath);
        var info = CertificateService.GetCurrentCertificateInfo(certPath);
        if (info is null)
        {
            return false;
        }

        var daysUntilExpiry = (info.NotAfter - time.GetUtcNow().UtcDateTime).TotalDays;
        if (daysUntilExpiry > RenewWithinDays)
        {
            logger.LogDebug("Certificate valid for {Days} more days, no renewal needed", (int)daysUntilExpiry);
            return false;
        }

        logger.LogInformation("Certificate expires in {Days} days, renewing", (int)daysUntilExpiry);
        var progress = new SyncProgress<CertificateProvisioningStatus>(status =>
            logger.LogInformation("Renewal: {Step} - {Message}", status.Step, status.Message));

        try
        {
            await certificates.ProvisionCertificateAsync(
                tls.Domain,
                tls.Email,
                tls.DnsProvider,
                credentials,
                certPath,
                CertificatePaths.AccountKey(options.Value, environment.ContentRootPath),
                progress,
                ct);
        }
        catch (Exceptions.ServiceValidationException)
        {
            logger.LogInformation("Another certificate request is running; renewal will be checked again tomorrow");
            return false;
        }

        serverCertificate.Load(certPath);
        logger.LogInformation("Certificate renewed");
        return true;
    }

    private async Task RunRenewalCheckAsync(CancellationToken stoppingToken)
    {
        try
        {
            await CheckAndRenewAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Certificate renewal check failed");
        }
    }
}

/// <summary>
/// An <see cref="IProgress{T}"/> that runs the callback on the reporting thread and never throws back into it.
/// </summary>
/// <typeparam name="T">Report type.</typeparam>
/// <param name="callback">Called for each report.</param>
public sealed class SyncProgress<T>(Action<T> callback) : IProgress<T>
{
    /// <inheritdoc />
    public void Report(T value)
    {
        try
        {
            callback(value);
        }
        catch
        {
            // Progress is informational; a failing listener must not fail the request.
        }
    }
}
