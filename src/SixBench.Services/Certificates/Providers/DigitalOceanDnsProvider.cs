using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace SixBench.Services.Certificates.Providers;

/// <summary>
/// DigitalOcean DNS (personal access token with write scope).
/// </summary>
/// <param name="http">HTTP client factory.</param>
public sealed class DigitalOceanDnsProvider(IHttpClientFactory http) : IDnsChallengeProvider
{
    private const string BaseUrl = "https://api.digitalocean.com/v2";

    /// <inheritdoc />
    public string ProviderName => DnsProviders.DigitalOcean;

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredCredentialKeys => ["ApiToken"];

    /// <inheritdoc />
    public async Task ValidateCredentialsAsync(IReadOnlyDictionary<string, string> credentials, CancellationToken ct = default)
    {
        using var client = CreateClient(credentials);
        var response = await client.GetAsync($"{BaseUrl}/account", ct);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task CreateTxtRecordAsync(string domain, string recordName, string recordValue, IReadOnlyDictionary<string, string> credentials, CancellationToken ct = default)
    {
        using var client = CreateClient(credentials);
        var rootDomain = DnsNames.GetRootDomain(domain);
        var payload = new { type = "TXT", name = DnsNames.GetRelativeRecordName(recordName, rootDomain), data = recordValue, ttl = 120 };
        var response = await client.PostAsJsonAsync($"{BaseUrl}/domains/{rootDomain}/records", payload, ct);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task DeleteTxtRecordAsync(string domain, string recordName, string recordValue, IReadOnlyDictionary<string, string> credentials, CancellationToken ct = default)
    {
        using var client = CreateClient(credentials);
        var rootDomain = DnsNames.GetRootDomain(domain);
        var relName = DnsNames.GetRelativeRecordName(recordName, rootDomain);

        foreach (var record in (await ListAsync(client, rootDomain, "TXT", ct)).Where(r => r.Name == relName && r.Data == recordValue))
        {
            await client.DeleteAsync($"{BaseUrl}/domains/{rootDomain}/records/{record.Id}", ct);
        }
    }

    /// <inheritdoc />
    public async Task UpsertARecordAsync(string domain, string hostname, string ipAddress, IReadOnlyDictionary<string, string> credentials, CancellationToken ct = default)
    {
        using var client = CreateClient(credentials);
        var rootDomain = DnsNames.GetRootDomain(domain);
        var relName = DnsNames.GetRelativeRecordName(hostname, rootDomain);

        var existing = (await ListAsync(client, rootDomain, "A", ct)).FirstOrDefault(r => r.Name == relName);
        HttpResponseMessage response;
        if (existing is not null)
        {
            using var patch = new HttpRequestMessage(HttpMethod.Patch, $"{BaseUrl}/domains/{rootDomain}/records/{existing.Id}")
            {
                Content = JsonContent.Create(new { data = ipAddress }),
            };
            response = await client.SendAsync(patch, ct);
        }
        else
        {
            response = await client.PostAsJsonAsync($"{BaseUrl}/domains/{rootDomain}/records", new { type = "A", name = relName, data = ipAddress, ttl = 300 }, ct);
        }

        response.EnsureSuccessStatusCode();
    }

    private HttpClient CreateClient(IReadOnlyDictionary<string, string> credentials)
    {
        var client = http.CreateClient(nameof(DigitalOceanDnsProvider));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credentials["ApiToken"]);
        return client;
    }

    private static async Task<Record[]> ListAsync(HttpClient client, string rootDomain, string type, CancellationToken ct)
    {
        var response = await client.GetAsync($"{BaseUrl}/domains/{rootDomain}/records?type={type}&per_page=200", ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RecordsResponse>(ct))?.DomainRecords ?? [];
    }

    private sealed record RecordsResponse([property: JsonPropertyName("domain_records")] Record[]? DomainRecords);

    private sealed record Record(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("data")] string Data);
}
