using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SixBench.Api.Hubs;
using SixBench.Common.Dtos;
using SixBench.Common.Security;
using SixBench.Services.Certificates;

namespace SixBench.Api.Controllers;

/// <summary>
/// HTTPS certificate from Let's Encrypt, verified through a DNS provider's API (administrators only).
/// </summary>
/// <param name="setup">Certificate setup service.</param>
/// <param name="providers">DNS providers.</param>
/// <param name="hub">Progress hub.</param>
/// <param name="http">HTTP client factory (public IP lookup).</param>
/// <param name="logger">Logger.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/certificates")]
[Produces("application/json")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class CertificatesController(
    CertificateSetupService setup,
    DnsProviderFactory providers,
    IHubContext<CertificateHub> hub,
    IHttpClientFactory http,
    ILogger<CertificatesController> logger) : ControllerBase
{
    /// <summary>
    /// Gets the installed certificate, whether it renews automatically, and whether HTTPS is active.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The state.</returns>
    [HttpGet("status")]
    [ProducesResponseType<CertificateStatusDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CertificateStatusDto>> Status(CancellationToken ct) => Ok(await setup.GetStatusAsync(ct));

    /// <summary>
    /// Lists the supported DNS providers and the credentials each needs.
    /// </summary>
    /// <returns>The providers.</returns>
    [HttpGet("providers")]
    [ProducesResponseType<IReadOnlyList<DnsProviderInfoDto>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<DnsProviderInfoDto>> Providers() => Ok(providers.GetAllProviders());

    /// <summary>
    /// Tests DNS provider credentials (the result says whether they work).
    /// </summary>
    /// <param name="request">Provider, credentials and domain.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result.</returns>
    [HttpPost("validate-credentials")]
    [ProducesResponseType<CredentialValidationResultDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CredentialValidationResultDto>> ValidateCredentials(ValidateCredentialsRequest request, CancellationToken ct) =>
        Ok(await setup.ValidateCredentialsAsync(request, ct));

    /// <summary>
    /// Looks up this server's public IP address (api.ipify.org, then ifconfig.me).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The address.</returns>
    [HttpGet("public-ip")]
    [ProducesResponseType<PublicIpDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<PublicIpDto>> PublicIp(CancellationToken ct)
    {
        using var client = http.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(10);
        foreach (var url in new[] { "https://api.ipify.org", "https://ifconfig.me/ip" })
        {
            try
            {
                return Ok(new PublicIpDto((await client.GetStringAsync(url, ct)).Trim()));
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                logger.LogDebug(ex, "Public IP lookup via {Url} failed", url);
            }
        }

        return Problem(statusCode: StatusCodes.Status502BadGateway, title: "Public IP unavailable", detail: "Could not detect the public IP address.");
    }

    /// <summary>
    /// Obtains and installs a certificate: optionally sets the A record, creates the TXT record, has Let's Encrypt
    /// validate it, saves the certificate and stores the credentials encrypted for renewal. Takes about a minute;
    /// progress is pushed on <c>/hubs/certificates</c>. HTTPS starts after a restart.
    /// </summary>
    /// <param name="request">Domain, email, DNS provider, credentials and A-record option.</param>
    /// <returns>The new state.</returns>
    [HttpPost("provision")]
    [ProducesResponseType<CertificateStatusDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<CertificateStatusDto>> Provision(ProvisionCertificateRequest request)
    {
        var progress = new SyncProgress<CertificateProvisioningStatus>(status =>
            Send(CertificateHub.ProgressEvent, new CertificateProgressDto(status.Step.ToString(), status.Message)));
        try
        {
            var status = await setup.ProvisionAsync(request, progress);
            Send(CertificateHub.CompleteEvent, new { success = true, message = "Certificate provisioned. DNS credentials were encrypted for automatic renewal." });
            return Ok(status);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Send(CertificateHub.CompleteEvent, new { success = false, message = ex.Message });
            throw;
        }
    }

    /// <summary>
    /// Deletes the certificate. Restart the server to go back to HTTP.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The new state.</returns>
    [HttpDelete]
    [ProducesResponseType<CertificateStatusDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CertificateStatusDto>> Remove(CancellationToken ct) => Ok(await setup.RemoveAsync(ct));

    /// <summary>Pushes to the hub without waiting; the HTTP response is the source of truth.</summary>
    private void Send(string method, object payload) =>
        _ = hub.Clients.All.SendAsync(method, payload).ContinueWith(
            t => logger.LogDebug(t.Exception, "Could not push {Method}", method),
            TaskContinuationOptions.OnlyOnFaulted);
}
