using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixBench.Common.Dtos;
using SixBench.Common.Options;
using SixBench.Services.Exceptions;

namespace SixBench.Services.Certificates;

/// <summary>
/// The HTTPS setup the web app drives: status, credential checks, provisioning and removal.
/// </summary>
/// <param name="certificates">Certificate service.</param>
/// <param name="settings">TLS settings.</param>
/// <param name="protector">Credential encryption.</param>
/// <param name="serverCertificate">The certificate Kestrel serves.</param>
/// <param name="runtime">How the server was bound at startup.</param>
/// <param name="options">TLS options.</param>
/// <param name="environment">Host environment.</param>
/// <param name="lifetime">Application lifetime.</param>
/// <param name="logger">Logger.</param>
public sealed class CertificateSetupService(
    CertificateService certificates,
    ITlsSettingsService settings,
    IDnsCredentialProtector protector,
    IServerCertificate serverCertificate,
    TlsRuntimeState runtime,
    IOptions<TlsOptions> options,
    IHostEnvironment environment,
    IHostApplicationLifetime lifetime,
    ILogger<CertificateSetupService> logger)
{
    // One request at a time.
    private static readonly SemaphoreSlim Gate = new(1, 1);

    private string CertPath => CertificatePaths.Certificate(options.Value, environment.ContentRootPath);

    /// <summary>
    /// Gets the certificate state. Never decrypts the stored credentials, so a lost key ring shows as "auto-renew off".
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The state.</returns>
    public async Task<CertificateStatusDto> GetStatusAsync(CancellationToken ct = default)
    {
        var info = CertificateService.GetCurrentCertificateInfo(CertPath);
        var tls = await settings.GetAsync(ct);
        var autoRenew = info is not null
            && tls.Enabled
            && !string.IsNullOrWhiteSpace(tls.Domain)
            && !string.IsNullOrWhiteSpace(tls.Email)
            && !string.IsNullOrWhiteSpace(tls.DnsProvider)
            && tls.EncryptedDnsCredentials.Count > 0;
        return new CertificateStatusDto(
            info is not null,
            info,
            autoRenew,
            runtime.HttpsEnabled,
            info is null ? null : $"https://{info.Domain}:{runtime.Port}",
            runtime.Port);
    }

    /// <summary>
    /// Tests DNS provider credentials.
    /// </summary>
    /// <param name="request">Provider, credentials and domain.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Whether they work, and why not.</returns>
    public async Task<CredentialValidationResultDto> ValidateCredentialsAsync(ValidateCredentialsRequest request, CancellationToken ct = default)
    {
        try
        {
            await certificates.ValidateProviderCredentialsAsync(request.DnsProvider, request.DnsCredentials, request.Domain, ct);
            return new CredentialValidationResultDto(true, "Credentials validated successfully.");
        }
        catch (HttpRequestException ex) when (ex.StatusCode is { } code)
        {
            var reason = code is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden or System.Net.HttpStatusCode.BadRequest
                ? "rejected the credentials"
                : "returned an error";
            return new CredentialValidationResultDto(false, $"{request.DnsProvider} {reason} (HTTP {(int)code}).");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new CredentialValidationResultDto(false, ex.Message);
        }
    }

    /// <summary>
    /// Optionally points the A record at the server, obtains the certificate, loads it, and saves the settings
    /// (credentials encrypted) so it renews automatically. Keeps running if the browser goes away.
    /// HTTPS is served after the next restart.
    /// </summary>
    /// <param name="request">Domain, email, provider, credentials and A-record option.</param>
    /// <param name="progress">Progress reports.</param>
    /// <returns>The new state.</returns>
    public async Task<CertificateStatusDto> ProvisionAsync(ProvisionCertificateRequest request, IProgress<CertificateProvisioningStatus> progress)
    {
        if (!await Gate.WaitAsync(0))
        {
            throw new ServiceValidationException("A certificate request is already in progress.");
        }

        try
        {
            var domain = request.Domain.Trim().TrimEnd('.').ToLowerInvariant();
            var email = request.Email.Trim();
            var ct = lifetime.ApplicationStopping;

            if (request.SetupDnsRecord && !string.IsNullOrWhiteSpace(request.PublicIp))
            {
                progress.Report(new(CertificateProvisioningStep.SettingARecord, $"Pointing {domain} to {request.PublicIp}..."));
                await certificates.SetARecordAsync(request.DnsProvider, domain, request.PublicIp.Trim(), request.DnsCredentials, ct);
                await Task.Delay(TimeSpan.FromSeconds(5), ct); // brief wait for the A record
            }

            await certificates.ProvisionCertificateAsync(
                domain,
                email,
                request.DnsProvider,
                request.DnsCredentials,
                CertPath,
                CertificatePaths.AccountKey(options.Value, environment.ContentRootPath),
                progress,
                ct);

            serverCertificate.Load(CertPath);
            await settings.SaveAsync(domain, email, request.DnsProvider, protector.Protect(request.DnsCredentials), CancellationToken.None);
            return await GetStatusAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Certificate provisioning failed");
            progress.Report(new(CertificateProvisioningStep.Failed, ex.Message));
            throw;
        }
        finally
        {
            Gate.Release();
        }
    }

    /// <summary>
    /// Deletes the certificate. The server keeps serving HTTPS with the loaded certificate until it restarts, then uses HTTP.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The new state.</returns>
    public async Task<CertificateStatusDto> RemoveAsync(CancellationToken ct = default)
    {
        if (File.Exists(CertPath))
        {
            File.Delete(CertPath);
            logger.LogInformation("Certificate removed. Restart the server to revert to HTTP.");
        }

        return await GetStatusAsync(ct);
    }
}
