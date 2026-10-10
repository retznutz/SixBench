using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SixBench.Services.Certificates.Providers;

/// <summary>
/// GoDaddy DNS (production API key and secret from developer.godaddy.com).
/// </summary>
/// <param name="http">HTTP client factory.</param>
public sealed class GoDaddyDnsProvider(IHttpClientFactory http) : IDnsChallengeProvider
{
    private const string BaseUrl = "https://api.godaddy.com/v1";

    /// <inheritdoc />
    public string ProviderName => DnsProviders.GoDaddy;

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredCredentialKeys => ["ApiKey", "ApiSecret"];

    /// <inheritdoc />
    public async Task ValidateCredentialsAsync(IReadOnlyDictionary<string, string> credentials, CancellationToken ct = default)
    {
        var domain = credentials.TryGetValue("Domain", out var d) && !string.IsNullOrWhiteSpace(d)
            ? DnsNames.GetRootDomain(d)
            : throw new InvalidOperationException("Domain is required to validate GoDaddy credentials.");

        using var client = CreateClient(credentials);
        var response = await client.GetAsync($"{BaseUrl}/domains/{domain}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException($"Domain '{domain}' was not found in your GoDaddy account. Make sure the root domain is registered with GoDaddy.");
        }

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new InvalidOperationException("Invalid API Key or Secret. Make sure you are using Production keys (not OTE/Test).");
        }

        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public Task CreateTxtRecordAsync(string domain, string recordName, string recordValue, IReadOnlyDictionary<string, string> credentials, CancellationToken ct = default) =>
        PutRecordAsync(domain, "TXT", recordName, recordValue, credentials, ct);

    /// <inheritdoc />
    public async Task DeleteTxtRecordAsync(string domain, string recordName, string recordValue, IReadOnlyDictionary<string, string> credentials, CancellationToken ct = default)
    {
        using var client = CreateClient(credentials);
        var rootDomain = DnsNames.GetRootDomain(domain);
        var relName = DnsNames.GetRelativeRecordName(recordName, rootDomain);

        // Removes every TXT record at the name (only our challenge lives there). Don't leave a placeholder value behind:
        // GoDaddy's nameservers and Let's Encrypt's resolvers can keep serving it for the 600s TTL, failing the next attempt.
        // 404 means there was nothing to delete.
        var response = await client.DeleteAsync($"{BaseUrl}/domains/{rootDomain}/records/TXT/{relName}", ct);
        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
        {
            response.EnsureSuccessStatusCode();
        }
    }

    /// <inheritdoc />
    public Task UpsertARecordAsync(string domain, string hostname, string ipAddress, IReadOnlyDictionary<string, string> credentials, CancellationToken ct = default) =>
        PutRecordAsync(domain, "A", hostname, ipAddress, credentials, ct);

    private async Task PutRecordAsync(
        string domain, string type, string name, string data, IReadOnlyDictionary<string, string> credentials, CancellationToken ct)
    {
        using var client = CreateClient(credentials);
        var rootDomain = DnsNames.GetRootDomain(domain);
        var relName = DnsNames.GetRelativeRecordName(name, rootDomain);

        // PUT replaces every record of this type and name.
        var response = await client.PutAsJsonAsync($"{BaseUrl}/domains/{rootDomain}/records/{type}/{relName}", new[] { new { data, ttl = 600 } }, ct);
        response.EnsureSuccessStatusCode();
    }

    private HttpClient CreateClient(IReadOnlyDictionary<string, string> credentials)
    {
        var client = http.CreateClient(nameof(GoDaddyDnsProvider));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("sso-key", $"{credentials["ApiKey"]}:{credentials["ApiSecret"]}");
        return client;
    }
}
