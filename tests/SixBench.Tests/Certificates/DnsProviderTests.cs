using System.Net;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using SixBench.Services.Certificates;
using SixBench.Services.Certificates.Providers;

namespace SixBench.Tests.Certificates;

public sealed class DnsProviderTests
{
    [Theory]
    [InlineData("sixbench.example.com", "example.com")]
    [InlineData("example.com", "example.com")]
    [InlineData("a.b.example.com.", "example.com")]
    public void Root_domain_is_the_last_two_labels(string domain, string root) => Assert.Equal(root, DnsNames.GetRootDomain(domain));

    [Theory]
    [InlineData("_acme-challenge.sixbench.example.com", "example.com", "_acme-challenge.sixbench")]
    [InlineData("_acme-challenge.example.com", "example.com", "_acme-challenge")]
    [InlineData("other.org", "example.com", "other.org")]
    public void Relative_record_names(string record, string root, string relative) =>
        Assert.Equal(relative, DnsNames.GetRelativeRecordName(record, root));

    [Fact]
    public async Task Cloudflare_finds_the_zone_then_creates_and_deletes_the_txt_record()
    {
        var http = new StubHttp(req => req.RequestUri!.PathAndQuery switch
        {
            "/client/v4/zones?name=example.com" => Json("""{"result":[{"id":"zone1"}]}"""),
            var p when p.StartsWith("/client/v4/zones/zone1/dns_records?type=TXT", StringComparison.Ordinal) => Json("""{"result":[{"id":"rec1"}]}"""),
            _ => Json("""{"result":[]}"""),
        });
        var provider = new CloudflareDnsProvider(http);
        var creds = new Dictionary<string, string> { ["ApiToken"] = "tok" };

        await provider.CreateTxtRecordAsync("sixbench.example.com", "_acme-challenge.sixbench.example.com", "v1", creds);
        await provider.DeleteTxtRecordAsync("sixbench.example.com", "_acme-challenge.sixbench.example.com", "v1", creds);

        var post = http.Requests.Single(r => r.Method == HttpMethod.Post);
        Assert.Equal("/client/v4/zones/zone1/dns_records", post.Path);
        Assert.Contains("\"name\":\"_acme-challenge.sixbench.example.com\"", post.Body, StringComparison.Ordinal);
        Assert.Contains("\"content\":\"v1\"", post.Body, StringComparison.Ordinal);
        Assert.Equal("Bearer tok", post.Authorization);
        Assert.Equal("/client/v4/zones/zone1/dns_records/rec1", http.Requests.Single(r => r.Method == HttpMethod.Delete).Path);
    }

    [Fact]
    public async Task GoDaddy_puts_relative_names_and_tolerates_422_on_cleanup()
    {
        var http = new StubHttp(req => req.Method == HttpMethod.Put && req.RequestUri!.AbsolutePath.EndsWith("/A/sixbench", StringComparison.Ordinal)
            ? new HttpResponseMessage(HttpStatusCode.OK)
            : req.Content is not null && Encoding.UTF8.GetString(req.Content.ReadAsByteArrayAsync().Result).Contains("deleted", StringComparison.Ordinal)
                ? new HttpResponseMessage((HttpStatusCode)422)
                : new HttpResponseMessage(HttpStatusCode.OK));
        var provider = new GoDaddyDnsProvider(http);
        var creds = new Dictionary<string, string> { ["ApiKey"] = "k", ["ApiSecret"] = "s" };

        await provider.CreateTxtRecordAsync("sixbench.example.com", "_acme-challenge.sixbench.example.com", "v1", creds);
        await provider.DeleteTxtRecordAsync("sixbench.example.com", "_acme-challenge.sixbench.example.com", "v1", creds);
        await provider.UpsertARecordAsync("sixbench.example.com", "sixbench.example.com", "203.0.113.5", creds);

        Assert.Equal("/v1/domains/example.com/records/TXT/_acme-challenge.sixbench", http.Requests[0].Path);
        Assert.Equal("sso-key k:s", http.Requests[0].Authorization);
        Assert.Equal("/v1/domains/example.com/records/A/sixbench", http.Requests[2].Path);
        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.ValidateCredentialsAsync(creds));
    }

    [Fact]
    public async Task DuckDns_sets_txt_for_the_subdomain()
    {
        var http = new StubHttp(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("OK") });
        var creds = new Dictionary<string, string> { ["Token"] = "t", ["Subdomain"] = "mybench.duckdns.org" };

        await new DuckDnsDnsProvider(http).CreateTxtRecordAsync("mybench.duckdns.org", "_acme-challenge.mybench.duckdns.org", "v+1", creds);

        Assert.Equal("/update?domains=mybench&token=t&txt=v%2B1&verbose=true", http.Requests[0].Path);
    }

    [Fact]
    public void Route53_signs_with_the_query_outside_the_canonical_path()
    {
        var at = new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);
        using var a = Route53DnsProvider.CreateSignedRequest(HttpMethod.Get, "/2013-04-01/hostedzone", "maxitems=1", null, "AKID", "secret", at);
        using var b = Route53DnsProvider.CreateSignedRequest(HttpMethod.Get, "/2013-04-01/hostedzone", "maxitems=2", null, "AKID", "secret", at);

        Assert.Equal("https://route53.amazonaws.com/2013-04-01/hostedzone?maxitems=1", a.RequestUri!.ToString());
        Assert.StartsWith("Credential=AKID/20261007/us-east-1/route53/aws4_request, SignedHeaders=host;x-amz-date, Signature=", a.Headers.Authorization!.Parameter, StringComparison.Ordinal);
        Assert.NotEqual(a.Headers.Authorization.Parameter, b.Headers.Authorization!.Parameter); // the query is signed
    }

    [Fact]
    public void Credentials_round_trip_through_data_protection_and_blank_values_are_dropped()
    {
        var protector = new DnsCredentialProtector(new EphemeralDataProtectionProvider());

        var encrypted = protector.Protect(new Dictionary<string, string> { ["ApiToken"] = "secret-token", ["Empty"] = " " });

        Assert.Equal(["ApiToken"], encrypted.Keys);
        Assert.DoesNotContain("secret-token", encrypted["ApiToken"], StringComparison.Ordinal);
        Assert.Equal("secret-token", protector.Unprotect(encrypted)["ApiToken"]);
    }

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    internal sealed record SeenRequest(HttpMethod Method, string Path, string Body, string? Authorization);

    internal sealed class StubHttp(Func<HttpRequestMessage, HttpResponseMessage> respond) : IHttpClientFactory
    {
        public List<SeenRequest> Requests { get; } = [];

        public HttpResponseMessage Respond(HttpRequestMessage request) => respond(request);

        public HttpClient CreateClient(string name) => new(new Handler(this));

        private sealed class Handler(StubHttp owner) : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
                owner.Requests.Add(new SeenRequest(request.Method, request.RequestUri!.PathAndQuery, body, request.Headers.Authorization?.ToString()));
                return owner.Respond(request);
            }
        }
    }
}
