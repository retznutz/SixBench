using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SixBench.Api.Infrastructure;
using SixBench.Data.Repositories;
using SixBench.Services.Certificates;
using SixBench.Services.Ffmpeg;
using SixBench.Tests.Api;

namespace SixBench.Tests.Certificates;

[Collection(ApiCollection.Name)]
public sealed class CertificatesApiTests(CertificatesApiTests.Factory factory) : IClassFixture<CertificatesApiTests.Factory>, IAsyncLifetime
{
    private HttpClient _admin = null!;

    public async Task InitializeAsync() => _admin = await factory.SignInAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Provision_installs_stores_encrypted_credentials_renews_and_removes()
    {
        // Admin only.
        await _admin.PostAsJsonAsync("/api/v1/users", new { userName = "viewer", password = "Viewer-pass-1!", role = "User" });
        var viewer = await factory.SignInAsync("viewer", "Viewer-pass-1!");
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.GetAsync("/api/v1/certificates/status")).StatusCode);

        var initial = await Status();
        Assert.False(initial.GetProperty("hasCertificate").GetBoolean());
        Assert.False(initial.GetProperty("httpsActive").GetBoolean());

        var providers = await _admin.GetFromJsonAsync<JsonElement>("/api/v1/certificates/providers");
        var names = providers.EnumerateArray().Select(p => p.GetProperty("name").GetString()).ToList();
        Assert.Contains("GoDaddy", names);
        Assert.Contains("Cloudflare", names);
        Assert.Contains("DuckDNS", names);
        Assert.Contains("Route53", names);
        Assert.Contains("DigitalOcean", names);

        // Credential checks report a result rather than an error.
        Assert.False(await Validate(new Dictionary<string, string> { ["Token"] = "wrong" }));
        Assert.False(await Validate(new Dictionary<string, string>()));
        Assert.True(await Validate(Creds));

        Assert.Equal(HttpStatusCode.BadRequest, (await Provision("not a domain")).StatusCode);

        // Provision with the A record; the certificate is close to expiry so the renewal job will pick it up.
        factory.Acme.Order.NotAfter = DateTimeOffset.UtcNow.AddDays(10);
        var response = await Provision("SixBench.Example.com");
        response.EnsureSuccessStatusCode();
        var status = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(status.GetProperty("hasCertificate").GetBoolean());
        Assert.True(status.GetProperty("autoRenewEnabled").GetBoolean());
        Assert.False(status.GetProperty("httpsActive").GetBoolean()); // until restart
        Assert.Equal("https://sixbench.example.com:5216", status.GetProperty("httpsUrl").GetString());
        Assert.Equal("sixbench.example.com", status.GetProperty("info").GetProperty("domain").GetString());
        Assert.Equal(
            ["A sixbench.example.com=203.0.113.5", "create _acme-challenge.sixbench.example.com=txt", "delete _acme-challenge.sixbench.example.com=txt"],
            factory.Provider.Calls);

        // Settings saved with encrypted credentials, and startup will see TLS as enabled.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var row = (await scope.ServiceProvider.GetRequiredService<ITlsSettingRepository>().GetAsync())!;
            Assert.True(row.Enabled);
            Assert.Equal("Fake", row.DnsProvider);
            Assert.DoesNotContain("super-secret", row.EncryptedDnsCredentials, StringComparison.Ordinal);
        }

        Assert.True(TlsStartupExtensions.ReadDatabaseOverride(factory.Configuration, factory.Root));
        Assert.Equal("CN=sixbench.example.com", factory.Services.GetRequiredService<IServerCertificate>().Current?.Subject);

        // Renewal decrypts the stored credentials and reloads the certificate.
        factory.Provider.Calls.Clear();
        factory.Acme.Order.NotAfter = DateTimeOffset.UtcNow.AddDays(90);
        var renewal = factory.Services.GetRequiredService<CertificateRenewalBackgroundService>();
        Assert.True(await renewal.CheckAndRenewAsync(CancellationToken.None));
        Assert.Contains("create _acme-challenge.sixbench.example.com=txt", factory.Provider.Calls);
        Assert.False(await renewal.CheckAndRenewAsync(CancellationToken.None)); // no longer due
        Assert.True((await Status()).GetProperty("info").GetProperty("notAfter").GetDateTime() > DateTime.UtcNow.AddDays(80));

        // Remove.
        var removed = await (await _admin.DeleteAsync("/api/v1/certificates")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(removed.GetProperty("hasCertificate").GetBoolean());
    }

    [Fact]
    public async Task Restart_is_admin_only()
    {
        var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsync("/api/v1/server/restart", null)).StatusCode);
    }

    private static readonly Dictionary<string, string> Creds = new() { ["Token"] = "t", ["Extra"] = "super-secret" };

    private Task<JsonElement> Status() => _admin.GetFromJsonAsync<JsonElement>("/api/v1/certificates/status");

    private async Task<bool> Validate(Dictionary<string, string> credentials)
    {
        var response = await _admin.PostAsJsonAsync("/api/v1/certificates/validate-credentials", new { dnsProvider = "Fake", dnsCredentials = credentials, domain = "sixbench.example.com" });
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("success").GetBoolean();
    }

    private Task<HttpResponseMessage> Provision(string domain) =>
        _admin.PostAsJsonAsync("/api/v1/certificates/provision", new
        {
            domain,
            email = "me@example.com",
            dnsProvider = "Fake",
            dnsCredentials = Creds,
            setupDnsRecord = true,
            publicIp = "203.0.113.5",
        });

    public sealed class Factory : WebApplicationFactory<Program>
    {
        public string Root { get; } = Directory.CreateTempSubdirectory("sixbench-tls-").FullName;

        public CertificateServiceTests.FakeProvider Provider { get; } = new();

        public CertificateServiceTests.FakeAcme Acme { get; } = new();

        public IConfiguration Configuration => Services.GetRequiredService<IConfiguration>();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:SixBench", $"Data Source={Path.Combine(Root, "test.db")}");
            builder.UseSetting("Serilog:WriteTo:1:Name", "Console");
            builder.UseSetting("Tls:CertificateDirectory", Path.Combine(Root, "certs"));
            builder.UseSetting("Tls:DnsSettleSeconds", "0");
            TestAuth.Configure(builder, Root);
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IDnsChallengeProvider>(Provider);
                services.RemoveAll<IAcmeClient>();
                services.AddSingleton<IAcmeClient>(Acme);
                services.RemoveAll<IDnsTxtChecker>();
                services.AddSingleton<IDnsTxtChecker>(new CertificateServiceTests.FakeDns());
                services.RemoveAll<IFfmpegCapabilities>();
                services.AddSingleton<IFfmpegCapabilities, FakeCapabilities>();
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            Directory.Delete(Root, recursive: true);
        }
    }

    private sealed class FakeCapabilities : IFfmpegCapabilities
    {
        public Task<FfmpegCapabilityReport> GetAsync(CancellationToken ct = default) =>
            Task.FromResult(new FfmpegCapabilityReport(true, "libx264", true, true, ["libx264"], null));
    }
}
