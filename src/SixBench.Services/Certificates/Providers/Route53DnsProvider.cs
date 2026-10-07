using System.Globalization;
using System.Net.Http.Headers;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace SixBench.Services.Certificates.Providers;

/// <summary>
/// Amazon Route 53 (IAM access key with <c>route53:ChangeResourceRecordSets</c> and <c>route53:ListHostedZones*</c>).
/// Requests are signed with AWS Signature Version 4.
/// </summary>
/// <param name="http">HTTP client factory.</param>
/// <param name="time">Clock (for request signing).</param>
public sealed class Route53DnsProvider(IHttpClientFactory http, TimeProvider time) : IDnsChallengeProvider
{
    private const string ServiceName = "route53";
    private const string Region = "us-east-1"; // Route 53 is global but signs in us-east-1.
    private const string Host = "route53.amazonaws.com";

    /// <inheritdoc />
    public string ProviderName => DnsProviders.Route53;

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredCredentialKeys => ["AccessKeyId", "SecretAccessKey"];

    /// <inheritdoc />
    public async Task ValidateCredentialsAsync(IReadOnlyDictionary<string, string> credentials, CancellationToken ct = default)
    {
        var response = await SendAsync(HttpMethod.Get, "/2013-04-01/hostedzone", "maxitems=1", null, credentials, ct);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task CreateTxtRecordAsync(string domain, string recordName, string recordValue, IReadOnlyDictionary<string, string> credentials, CancellationToken ct = default)
    {
        var response = await ChangeAsync(domain, "UPSERT", recordName, "TXT", 120, $"\"{recordValue}\"", credentials, ct);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task DeleteTxtRecordAsync(string domain, string recordName, string recordValue, IReadOnlyDictionary<string, string> credentials, CancellationToken ct = default) =>
        // A failed DELETE (record already gone) is fine for cleanup.
        await ChangeAsync(domain, "DELETE", recordName, "TXT", 120, $"\"{recordValue}\"", credentials, ct);

    /// <inheritdoc />
    public async Task UpsertARecordAsync(string domain, string hostname, string ipAddress, IReadOnlyDictionary<string, string> credentials, CancellationToken ct = default)
    {
        var response = await ChangeAsync(domain, "UPSERT", hostname, "A", 300, ipAddress, credentials, ct);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Builds a SigV4-signed request (exposed for tests).
    /// </summary>
    /// <param name="method">HTTP method.</param>
    /// <param name="path">Absolute path, e.g. <c>/2013-04-01/hostedzone</c>.</param>
    /// <param name="query">Canonical query string (already sorted and encoded), or empty.</param>
    /// <param name="body">XML body, or null.</param>
    /// <param name="accessKey">Access key id.</param>
    /// <param name="secretKey">Secret access key.</param>
    /// <param name="now">Signing time (UTC).</param>
    /// <returns>The signed request.</returns>
    public static HttpRequestMessage CreateSignedRequest(
        HttpMethod method, string path, string query, string? body, string accessKey, string secretKey, DateTime now)
    {
        var dateStamp = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var amzDate = now.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

        var request = new HttpRequestMessage(method, $"https://{Host}{path}{(query.Length > 0 ? "?" + query : string.Empty)}");
        request.Headers.Host = Host;
        request.Headers.Add("x-amz-date", amzDate);
        if (body is not null)
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/xml");
        }

        const string signedHeaders = "host;x-amz-date";
        var canonicalRequest = $"{method.Method}\n{path}\n{query}\nhost:{Host}\nx-amz-date:{amzDate}\n\n{signedHeaders}\n{Hash(body ?? string.Empty)}";
        var credentialScope = $"{dateStamp}/{Region}/{ServiceName}/aws4_request";
        var stringToSign = $"AWS4-HMAC-SHA256\n{amzDate}\n{credentialScope}\n{Hash(canonicalRequest)}";

        var signingKey = Hmac(Hmac(Hmac(Hmac(Encoding.UTF8.GetBytes($"AWS4{secretKey}"), dateStamp), Region), ServiceName), "aws4_request");
        var signature = Convert.ToHexStringLower(Hmac(signingKey, stringToSign));

        request.Headers.Authorization = new AuthenticationHeaderValue(
            "AWS4-HMAC-SHA256", $"Credential={accessKey}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}");
        return request;
    }

    private async Task<HttpResponseMessage> ChangeAsync(
        string domain, string action, string name, string type, int ttl, string value, IReadOnlyDictionary<string, string> credentials, CancellationToken ct)
    {
        var zoneId = await GetHostedZoneIdAsync(domain, credentials, ct);
        var xml = $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <ChangeResourceRecordSetsRequest xmlns="https://route53.amazonaws.com/doc/2013-04-01/">
              <ChangeBatch>
                <Changes>
                  <Change>
                    <Action>{action}</Action>
                    <ResourceRecordSet>
                      <Name>{SecurityElement.Escape(name)}</Name>
                      <Type>{type}</Type>
                      <TTL>{ttl}</TTL>
                      <ResourceRecords>
                        <ResourceRecord>
                          <Value>{SecurityElement.Escape(value)}</Value>
                        </ResourceRecord>
                      </ResourceRecords>
                    </ResourceRecordSet>
                  </Change>
                </Changes>
              </ChangeBatch>
            </ChangeResourceRecordSetsRequest>
            """;
        return await SendAsync(HttpMethod.Post, $"/2013-04-01/hostedzone/{zoneId}/rrset/", string.Empty, xml, credentials, ct);
    }

    private async Task<string> GetHostedZoneIdAsync(string domain, IReadOnlyDictionary<string, string> credentials, CancellationToken ct)
    {
        var rootDomain = DnsNames.GetRootDomain(domain);
        var response = await SendAsync(HttpMethod.Get, "/2013-04-01/hostedzonesbyname", $"dnsname={Uri.EscapeDataString(rootDomain)}", null, credentials, ct);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(ct);
        const string marker = "<Id>/hostedzone/";
        var start = content.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new InvalidOperationException($"Hosted zone not found for {rootDomain}");
        }

        start += marker.Length;
        return content[start..content.IndexOf("</Id>", start, StringComparison.Ordinal)];
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string path, string query, string? body, IReadOnlyDictionary<string, string> credentials, CancellationToken ct)
    {
        using var client = http.CreateClient(nameof(Route53DnsProvider));
        using var request = CreateSignedRequest(method, path, query, body, credentials["AccessKeyId"], credentials["SecretAccessKey"], time.GetUtcNow().UtcDateTime);
        return await client.SendAsync(request, ct);
    }

    private static string Hash(string text) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    private static byte[] Hmac(byte[] key, string data) => HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(data));
}
