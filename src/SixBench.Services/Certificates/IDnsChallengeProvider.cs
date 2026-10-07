namespace SixBench.Services.Certificates;

/// <summary>
/// Manages DNS records through a DNS provider's API, for the DNS-01 challenge and the optional A record.
/// </summary>
public interface IDnsChallengeProvider
{
    /// <summary>Provider name (see <see cref="DnsProviders"/>).</summary>
    string ProviderName { get; }

    /// <summary>Credential keys the provider needs.</summary>
    IReadOnlyList<string> RequiredCredentialKeys { get; }

    /// <summary>Checks that the credentials work.</summary>
    /// <param name="credentials">Credential key → value (may include <c>Domain</c>).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that fails if the credentials don't work.</returns>
    Task ValidateCredentialsAsync(IReadOnlyDictionary<string, string> credentials, CancellationToken ct = default);

    /// <summary>Creates a TXT record.</summary>
    /// <param name="domain">The certificate's domain.</param>
    /// <param name="recordName">Fully qualified record name, e.g. <c>_acme-challenge.sixbench.example.com</c>.</param>
    /// <param name="recordValue">Record value.</param>
    /// <param name="credentials">Credentials.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the record is created.</returns>
    Task CreateTxtRecordAsync(string domain, string recordName, string recordValue, IReadOnlyDictionary<string, string> credentials, CancellationToken ct = default);

    /// <summary>Deletes a TXT record created by <see cref="CreateTxtRecordAsync"/>.</summary>
    /// <param name="domain">The certificate's domain.</param>
    /// <param name="recordName">Fully qualified record name.</param>
    /// <param name="recordValue">Record value.</param>
    /// <param name="credentials">Credentials.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the record is deleted.</returns>
    Task DeleteTxtRecordAsync(string domain, string recordName, string recordValue, IReadOnlyDictionary<string, string> credentials, CancellationToken ct = default);

    /// <summary>Creates or updates an A record.</summary>
    /// <param name="domain">The certificate's domain.</param>
    /// <param name="hostname">Fully qualified host name.</param>
    /// <param name="ipAddress">IPv4 address.</param>
    /// <param name="credentials">Credentials.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the record is saved.</returns>
    Task UpsertARecordAsync(string domain, string hostname, string ipAddress, IReadOnlyDictionary<string, string> credentials, CancellationToken ct = default);
}

/// <summary>
/// Looks up DNS providers by name.
/// </summary>
/// <param name="providers">All registered providers.</param>
public sealed class DnsProviderFactory(IEnumerable<IDnsChallengeProvider> providers)
{
    private readonly Dictionary<string, IDnsChallengeProvider> _providers =
        providers.ToDictionary(p => p.ProviderName, StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets a provider.</summary>
    /// <param name="providerName">Provider name.</param>
    /// <returns>The provider.</returns>
    /// <exception cref="Exceptions.ServiceValidationException">Unknown provider.</exception>
    public IDnsChallengeProvider GetProvider(string providerName) =>
        _providers.TryGetValue(providerName, out var provider)
            ? provider
            : throw new Exceptions.ServiceValidationException(
                $"Unknown DNS provider: '{providerName}'. Supported providers: {string.Join(", ", _providers.Keys)}");

    /// <summary>Lists the providers and the credentials each needs.</summary>
    /// <returns>The providers.</returns>
    public IReadOnlyList<Common.Dtos.DnsProviderInfoDto> GetAllProviders() =>
        _providers.Values.Select(p => new Common.Dtos.DnsProviderInfoDto(p.ProviderName, p.RequiredCredentialKeys)).ToList();
}
