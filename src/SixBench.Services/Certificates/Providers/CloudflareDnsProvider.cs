using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace SixBench.Services.Certificates.Providers;

/// <summary>
/// Cloudflare DNS (API token with Zone › DNS › Edit).
/// </summary>
/// <param name="http">HTTP client factory.</param>
public sealed class CloudflareDnsProvider(IHttpClientFactory http) : IDnsChallengeProvider
{
    private const string BaseUrl = "https://api.cloudflare.com/client/v4";

    /// <inheritdoc />
    public string ProviderName => DnsProviders.Cloudflare;

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredCredentialKeys => ["ApiToken"];

    /// <inheritdoc />
    public async Task ValidateCredentialsAsync(IReadOnlyDictionary<string, string> credentials, CancellationToken ct = default)
    {
        using var client = CreateClient(credentials);
        var response = await client.GetAsync($"{BaseUrl}/user/tokens/verify", ct);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task CreateTxtRecordAsync(string domain, string recordName, string recordValue, IReadOnlyDictionary<string, string> credentials, CancellationToken ct = default)
    {
        using var client = CreateClient(credentials);
        var zoneId = await GetZoneIdAsync(client, domain, ct);

        var payload = new { type = "TXT", name = recordName, content = recordValue, ttl = 120 };
        var response = await client.PostAsJsonAsync($"{BaseUrl}/zones/{zoneId}/dns_records", payload, ct);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task DeleteTxtRecordAsync(string domain, string recordName, string recordValue, IReadOnlyDictionary<string, string> credentials, CancellationToken ct = default)
    {
        using var client = CreateClient(credentials);
        var zoneId = await GetZoneIdAsync(client, domain, ct);

        var response = await client.GetAsync(
            $"{BaseUrl}/zones/{zoneId}/dns_records?type=TXT&name={Uri.EscapeDataString(recordName)}&content={Uri.EscapeDataString(recordValue)}", ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ListResponse>(ct);
        foreach (var record in result?.Result ?? [])
        {
            await client.DeleteAsync($"{BaseUrl}/zones/{zoneId}/dns_records/{record.Id}", ct);
        }
    }

    /// <inheritdoc />
    public async Task UpsertARecordAsync(string domain, string hostname, string ipAddress, IReadOnlyDictionary<string, string> credentials, CancellationToken ct = default)
    {
        using var client = CreateClient(credentials);
        var zoneId = await GetZoneIdAsync(client, domain, ct);

        var response = await client.GetAsync($"{BaseUrl}/zones/{zoneId}/dns_records?type=A&name={Uri.EscapeDataString(hostname)}", ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ListResponse>(ct);

        var payload = new { type = "A", name = hostname, content = ipAddress, ttl = 300, proxied = false };
        var save = result?.Result is { Length: > 0 } existing
            ? await client.PutAsJsonAsync($"{BaseUrl}/zones/{zoneId}/dns_records/{existing[0].Id}", payload, ct)
            : await client.PostAsJsonAsync($"{BaseUrl}/zones/{zoneId}/dns_records", payload, ct);
        save.EnsureSuccessStatusCode();
    }

    private HttpClient CreateClient(IReadOnlyDictionary<string, string> credentials)
    {
        var client = http.CreateClient(nameof(CloudflareDnsProvider));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credentials["ApiToken"]);
        return client;
    }

    private static async Task<string> GetZoneIdAsync(HttpClient client, string domain, CancellationToken ct)
    {
        var rootDomain = DnsNames.GetRootDomain(domain);
        var response = await client.GetAsync($"{BaseUrl}/zones?name={Uri.EscapeDataString(rootDomain)}", ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ListResponse>(ct);
        return result?.Result?.FirstOrDefault()?.Id
            ?? throw new InvalidOperationException($"Zone not found for domain: {rootDomain}");
    }

    private sealed record ListResponse([property: JsonPropertyName("result")] Record[]? Result);

    private sealed record Record([property: JsonPropertyName("id")] string Id);
}
