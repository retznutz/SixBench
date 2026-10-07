using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SixBench.Common.Options;
using SixBench.Services.Certificates;
using SixBench.Services.Exceptions;

namespace SixBench.Tests.Certificates;

public sealed class CertificateServiceTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("sixbench-certsvc-").FullName;
    private readonly FakeProvider _provider = new();
    private readonly FakeAcme _acme = new();
    private readonly FakeDns _dns = new();

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public async Task Creates_record_waits_validates_saves_pfx_and_cleans_up()
    {
        _dns.VisibleAfterChecks = 1;
        var steps = new List<CertificateProvisioningStep>();

        var path = await Service().ProvisionCertificateAsync("sixbench.example.com", "me@example.com", "Fake", Creds, Pfx, Account,
            new SyncProgress<CertificateProvisioningStatus>(s => steps.Add(s.Step)));

        Assert.Equal(["create _acme-challenge.sixbench.example.com=txt", "delete _acme-challenge.sixbench.example.com=txt"], _provider.Calls);
        Assert.True(_acme.Order.Validated);
        Assert.True(File.Exists(path));
        Assert.Equal(
            [CertificateProvisioningStep.CreatingOrder, CertificateProvisioningStep.SettingDnsRecord, CertificateProvisioningStep.WaitingForPropagation,
             CertificateProvisioningStep.Validating, CertificateProvisioningStep.IssuingCertificate, CertificateProvisioningStep.Complete],
            steps);
        if (!OperatingSystem.IsWindows())
        {
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(path));
        }
    }

    [Fact]
    public async Task Record_that_never_appears_fails_without_asking_lets_encrypt_and_is_cleaned_up()
    {
        _dns.VisibleAfterChecks = int.MaxValue;

        var ex = await Assert.ThrowsAsync<CertificateRequestException>(() => Provision(timeoutSeconds: 10));

        Assert.Contains("was not asked", ex.Message, StringComparison.Ordinal);
        Assert.False(_acme.Order.Validated);
        Assert.Contains(_provider.Calls, c => c.StartsWith("delete", StringComparison.Ordinal));
        Assert.False(File.Exists(Pfx));
    }

    [Fact]
    public async Task Failed_validation_still_removes_the_record()
    {
        _acme.Order.FailValidation = true;

        await Assert.ThrowsAsync<CertificateRequestException>(() => Provision());

        Assert.Contains(_provider.Calls, c => c.StartsWith("delete", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Unreadable_dns_falls_back_to_a_fixed_wait()
    {
        _dns.Throw = true;
        CertificateService.BlindPropagationWait = TimeSpan.Zero;

        await Provision();

        Assert.True(_acme.Order.Validated);
    }

    [Fact]
    public async Task Already_validated_domain_skips_dns()
    {
        _acme.Order.RecordValue = null;

        await Provision();

        Assert.Empty(_provider.Calls);
        Assert.True(File.Exists(Pfx));
    }

    [Fact]
    public async Task Only_one_request_runs_at_a_time()
    {
        _acme.Order.BlockValidation = new TaskCompletionSource();
        var service = Service();
        var first = service.ProvisionCertificateAsync("sixbench.example.com", "me@example.com", "Fake", Creds, Pfx, Account, new SyncProgress<CertificateProvisioningStatus>(_ => { }));
        await _acme.Order.ValidationStarted.Task;

        await Assert.ThrowsAsync<ServiceValidationException>(() =>
            service.ProvisionCertificateAsync("sixbench.example.com", "me@example.com", "Fake", Creds, Pfx, Account, new SyncProgress<CertificateProvisioningStatus>(_ => { })));

        _acme.Order.BlockValidation.SetResult();
        await first;
    }

    [Fact]
    public async Task Provider_errors_are_reported_as_certificate_failures()
    {
        _provider.FailCreate = true;

        var ex = await Assert.ThrowsAsync<CertificateRequestException>(() => Provision());
        Assert.StartsWith("Fake DNS update failed", ex.Message, StringComparison.Ordinal);
        await Assert.ThrowsAsync<ServiceValidationException>(() => Service().ValidateProviderCredentialsAsync("Fake", new Dictionary<string, string>(), null));
        await Assert.ThrowsAsync<ServiceValidationException>(() => Service().ValidateProviderCredentialsAsync("Nope", Creds, null));
    }

    private static readonly Dictionary<string, string> Creds = new() { ["Token"] = "t" };

    private string Pfx => Path.Combine(_dir, "sixbench-cert.pfx");

    private string Account => Path.Combine(_dir, "acme-account.pem");

    private Task<string> Provision(int timeoutSeconds = 120) =>
        Service(timeoutSeconds).ProvisionCertificateAsync("sixbench.example.com", "me@example.com", "Fake", Creds, Pfx, Account, new SyncProgress<CertificateProvisioningStatus>(_ => { }));

    private CertificateService Service(int timeoutSeconds = 120) => new(
        new DnsProviderFactory([_provider]),
        _acme,
        _dns,
        Options.Create(new TlsOptions { ValidationTimeoutSeconds = timeoutSeconds }),
        NullLogger<CertificateService>.Instance);

    public sealed class FakeProvider : IDnsChallengeProvider
    {
        public List<string> Calls { get; } = [];

        public bool FailCreate { get; set; }

        public string ProviderName => "Fake";

        public IReadOnlyList<string> RequiredCredentialKeys => ["Token"];

        public Task ValidateCredentialsAsync(IReadOnlyDictionary<string, string> credentials, CancellationToken ct = default) =>
            credentials.GetValueOrDefault("Token") == "t" ? Task.CompletedTask : throw new InvalidOperationException("Bad token");

        public Task CreateTxtRecordAsync(string domain, string recordName, string recordValue, IReadOnlyDictionary<string, string> credentials, CancellationToken ct = default)
        {
            if (FailCreate)
            {
                throw new HttpRequestException("403 Forbidden");
            }

            Calls.Add($"create {recordName}={recordValue}");
            return Task.CompletedTask;
        }

        public Task DeleteTxtRecordAsync(string domain, string recordName, string recordValue, IReadOnlyDictionary<string, string> credentials, CancellationToken ct = default)
        {
            Calls.Add($"delete {recordName}={recordValue}");
            return Task.CompletedTask;
        }

        public Task UpsertARecordAsync(string domain, string hostname, string ipAddress, IReadOnlyDictionary<string, string> credentials, CancellationToken ct = default)
        {
            Calls.Add($"A {hostname}={ipAddress}");
            return Task.CompletedTask;
        }
    }

    public sealed class FakeAcme : IAcmeClient
    {
        public FakeOrder Order { get; } = new();

        public Task<IAcmeDnsOrder> CreateOrderAsync(string domain, string email, string accountKeyPath, bool useStaging, CancellationToken ct = default)
        {
            Order.Domain = domain;
            return Task.FromResult<IAcmeDnsOrder>(Order);
        }
    }

    public sealed class FakeOrder : IAcmeDnsOrder
    {
        public string Domain { get; set; } = string.Empty;

        public string? RecordValue { get; set; } = "txt";

        public bool Validated { get; private set; }

        public bool FailValidation { get; set; }

        public DateTimeOffset NotAfter { get; set; } = DateTimeOffset.UtcNow.AddDays(90);

        public TaskCompletionSource? BlockValidation { get; set; }

        public TaskCompletionSource ValidationStarted { get; } = new();

        public async Task ValidateAsync(TimeSpan timeout, CancellationToken ct = default)
        {
            Validated = true;
            ValidationStarted.TrySetResult();
            if (BlockValidation is not null)
            {
                await BlockValidation.Task;
            }

            if (FailValidation)
            {
                throw new CertificateRequestException("Domain validation failed.");
            }
        }

        public Task<byte[]> IssuePfxAsync(CancellationToken ct = default) =>
            Task.FromResult(TestCertificates.CreatePfx(Domain, DateTimeOffset.UtcNow.AddDays(-1), NotAfter));
    }

    public sealed class FakeDns : IDnsTxtChecker
    {
        private int _checks;

        public int VisibleAfterChecks { get; set; } = 1;

        public bool Throw { get; set; }

        public Task<DnsTxtCheckResult> CheckAsync(string name, string expectedValue, CancellationToken ct = default)
        {
            if (Throw)
            {
                throw new DnsClient.DnsResponseException("no resolver");
            }

            var found = ++_checks >= VisibleAfterChecks;
            return Task.FromResult(new DnsTxtCheckResult(found, found ? [expectedValue] : [], "test DNS"));
        }
    }
}
