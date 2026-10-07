using System.Text.Json;
using Microsoft.Extensions.Options;
using SixBench.Common.Options;
using SixBench.Data.Entities;
using SixBench.Data.Repositories;

namespace SixBench.Services.Certificates;

/// <summary>
/// The TLS settings in effect: the row saved from the web app if there is one, otherwise the <c>Tls</c> configuration section.
/// </summary>
/// <param name="Enabled">Serve HTTPS when the certificate exists.</param>
/// <param name="Domain">Certificate domain.</param>
/// <param name="Email">Let's Encrypt account email.</param>
/// <param name="DnsProvider">DNS provider name.</param>
/// <param name="EncryptedDnsCredentials">Credentials encrypted with <see cref="IDnsCredentialProtector"/>.</param>
/// <param name="LegacyDnsCredentials">Plain-text credentials from configuration (legacy).</param>
public sealed record TlsState(
    bool Enabled,
    string? Domain,
    string? Email,
    string? DnsProvider,
    IReadOnlyDictionary<string, string> EncryptedDnsCredentials,
    IReadOnlyDictionary<string, string> LegacyDnsCredentials);

/// <summary>
/// Reads and saves the TLS settings.
/// </summary>
public interface ITlsSettingsService
{
    /// <summary>Gets the settings in effect.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The settings.</returns>
    Task<TlsState> GetAsync(CancellationToken ct = default);

    /// <summary>Saves the settings after a certificate is provisioned and enables TLS.</summary>
    /// <param name="domain">Domain.</param>
    /// <param name="email">Email.</param>
    /// <param name="dnsProvider">DNS provider.</param>
    /// <param name="encryptedCredentials">Encrypted DNS credentials.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when saved.</returns>
    Task SaveAsync(string domain, string email, string dnsProvider, IReadOnlyDictionary<string, string> encryptedCredentials, CancellationToken ct = default);
}

/// <summary>
/// Default <see cref="ITlsSettingsService"/>.
/// </summary>
/// <param name="repository">The saved row.</param>
/// <param name="options">The <c>Tls</c> configuration section.</param>
/// <param name="time">Clock.</param>
public sealed class TlsSettingsService(ITlsSettingRepository repository, IOptions<TlsOptions> options, TimeProvider time) : ITlsSettingsService
{
    /// <inheritdoc />
    public async Task<TlsState> GetAsync(CancellationToken ct = default)
    {
        var config = options.Value;
        var row = await repository.GetAsync(ct);
        if (row is null)
        {
            return new TlsState(config.Enabled, config.Domain, config.Email, config.DnsProvider, new Dictionary<string, string>(), config.DnsCredentials);
        }

        var encrypted = string.IsNullOrWhiteSpace(row.EncryptedDnsCredentials)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(row.EncryptedDnsCredentials) ?? [];
        return new TlsState(row.Enabled, row.Domain, row.Email, row.DnsProvider, encrypted, config.DnsCredentials);
    }

    /// <inheritdoc />
    public async Task SaveAsync(string domain, string email, string dnsProvider, IReadOnlyDictionary<string, string> encryptedCredentials, CancellationToken ct = default)
    {
        await repository.SaveAsync(new TlsSetting
        {
            Enabled = true,
            Domain = domain,
            Email = email,
            DnsProvider = dnsProvider,
            EncryptedDnsCredentials = JsonSerializer.Serialize(encryptedCredentials),
            UpdatedUtc = time.GetUtcNow().UtcDateTime,
        }, ct);
    }
}
