namespace SixBench.Services.Certificates.Providers;

/// <summary>
/// DuckDNS (account token plus subdomain, without <c>.duckdns.org</c>).
/// </summary>
/// <param name="http">HTTP client factory.</param>
public sealed class DuckDnsDnsProvider(IHttpClientFactory http) : IDnsChallengeProvider
{
    private const string BaseUrl = "https://www.duckdns.org/update";

    /// <inheritdoc />
    public string ProviderName => DnsProviders.DuckDns;

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredCredentialKeys => ["Token", "Subdomain"];

    /// <inheritdoc />
    public async Task ValidateCredentialsAsync(IReadOnlyDictionary<string, string> credentials, CancellationToken ct = default)
    {
        var response = await SendAsync(credentials, "txt=validation_test", ct);
        if (!response.StartsWith("OK", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("DuckDNS credential validation failed. Check your token and subdomain.");
        }
    }

    /// <inheritdoc />
    public async Task CreateTxtRecordAsync(string domain, string recordName, string recordValue, IReadOnlyDictionary<string, string> credentials, CancellationToken ct = default)
    {
        var response = await SendAsync(credentials, $"txt={Uri.EscapeDataString(recordValue)}", ct);
        if (!response.StartsWith("OK", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Failed to set DuckDNS TXT record: {response}");
        }
    }

    /// <inheritdoc />
    public async Task DeleteTxtRecordAsync(string domain, string recordName, string recordValue, IReadOnlyDictionary<string, string> credentials, CancellationToken ct = default) =>
        await SendAsync(credentials, "txt=&clear=true", ct);

    /// <inheritdoc />
    public async Task UpsertARecordAsync(string domain, string hostname, string ipAddress, IReadOnlyDictionary<string, string> credentials, CancellationToken ct = default)
    {
        var response = await SendAsync(credentials, $"ip={Uri.EscapeDataString(ipAddress)}", ct);
        if (!response.StartsWith("OK", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Failed to update DuckDNS A record: {response}");
        }
    }

    private async Task<string> SendAsync(IReadOnlyDictionary<string, string> credentials, string query, CancellationToken ct)
    {
        using var client = http.CreateClient(nameof(DuckDnsDnsProvider));
        var url = $"{BaseUrl}?domains={Uri.EscapeDataString(GetSubdomain(credentials))}&token={Uri.EscapeDataString(credentials["Token"])}&{query}&verbose=true";
        return await client.GetStringAsync(url, ct);
    }

    private static string GetSubdomain(IReadOnlyDictionary<string, string> credentials) =>
        credentials.TryGetValue("Subdomain", out var sub) && !string.IsNullOrWhiteSpace(sub)
            ? sub.Replace(".duckdns.org", string.Empty, StringComparison.OrdinalIgnoreCase).Trim()
            : throw new InvalidOperationException("DuckDNS requires a 'Subdomain' credential.");
}
