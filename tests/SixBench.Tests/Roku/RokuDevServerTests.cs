using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SixBench.Common.Dtos;
using SixBench.Common.Options;
using SixBench.Data.Entities;
using SixBench.Data.Repositories;
using SixBench.Services.Exceptions;
using SixBench.Services.Roku;

namespace SixBench.Tests.Roku;

public class HttpDigestTests
{
    [Fact]
    public void Matches_the_rfc_2617_example()
    {
        var challenge = HttpDigest.ParseParameters(
            "realm=\"testrealm@host.com\", qop=\"auth,auth-int\", nonce=\"dcd98b7102dd2f0e8b11d0f600bfb0c093\", " +
            "opaque=\"5ccc069c403ebaf9f0171e9517f40e41\"");

        var header = HttpDigest.CreateAuthorization(challenge, "GET", "/dir/index.html", "Mufasa", "Circle Of Life", "0a4f113b");
        var parameters = HttpDigest.ParseParameters(header);

        Assert.Equal("6629fae49393a05397450978507c4ef1", parameters["response"]);
        Assert.Equal("auth", parameters["qop"]);
        Assert.Equal("00000001", parameters["nc"]);
        Assert.Equal("5ccc069c403ebaf9f0171e9517f40e41", parameters["opaque"]);
        Assert.Equal("/dir/index.html", parameters["uri"]);
    }

    [Fact]
    public void Parses_quoted_values_containing_commas_and_escapes()
    {
        var parameters = HttpDigest.ParseParameters("realm=\"a, b\", nonce=abc , opaque=\"x\\\"y\"");

        Assert.Equal("a, b", parameters["realm"]);
        Assert.Equal("abc", parameters["nonce"]);
        Assert.Equal("x\"y", parameters["opaque"]);
    }

    [Fact]
    public void Rejects_unsupported_algorithms()
    {
        var challenge = HttpDigest.ParseParameters("realm=\"r\", nonce=\"n\", algorithm=SHA-256");

        Assert.Throws<NotSupportedException>(() => HttpDigest.CreateAuthorization(challenge, "GET", "/", "u", "p", "c"));
    }
}

public class RokuDevServerParserTests
{
    [Fact]
    public void Reads_shell_messages()
    {
        const string html = """
            <script>
            Shell.create('Roku.Message').trigger('Set message type', 'success').trigger('Set message content', 'Received 5 bytes.').trigger('Render', node);
            Shell.create('Roku.Message').trigger('Set message type', 'error').trigger('Set message content', 'Install Failure: Compilation Failed.').trigger('Render', node);
            </script>
            """;

        var messages = RokuDevServerParser.ParseMessages(html);

        Assert.Equal(
            [new RokuDevMessageDto("success", "Received 5 bytes."), new RokuDevMessageDto("error", "Install Failure: Compilation Failed.")],
            messages);
    }

    [Fact]
    public void Reads_json_messages_with_js_escapes()
    {
        const string html = """
            <script>
            var params = JSON.parse('{"messages":[{"text":"Application Received: 2 bytes stored.","text_type":"text","type":"info"},{"text":"It\'s \\"done\\"","text_type":"text","type":"success"},{"text":"<b>x</b>","text_type":"html","type":"info"}],"metadata":{}}');
            </script>
            """;

        var messages = RokuDevServerParser.ParseMessages(html);

        Assert.Equal(
            [new RokuDevMessageDto("info", "Application Received: 2 bytes stored."), new RokuDevMessageDto("success", "It's \"done\"")],
            messages);
    }

    [Fact]
    public void Reads_font_status_and_dedupes()
    {
        const string html = "<font color=\"red\">Failed: Invalid Password.</font><font color=\"red\">Failed: Invalid Password.</font>";

        var message = Assert.Single(RokuDevServerParser.ParseMessages(html));

        Assert.Equal(new RokuDevMessageDto("error", "Failed: Invalid Password."), message);
        Assert.Equal("Failed: Invalid Password.", RokuDevServerParser.FindFontStatus(html));
    }

    [Theory]
    [InlineData("<img src=\"pkgs/dev.jpg?time=1672980398\">", "pkgs/dev.jpg?time=1672980398")]
    [InlineData("screenshot: 'pkgs/dev.png?time=5'", "pkgs/dev.png?time=5")]
    [InlineData("<p>nothing</p>", null)]
    public void Finds_screenshot_path(string html, string? expected) =>
        Assert.Equal(expected, RokuDevServerParser.FindScreenshotPath(html));

    [Theory]
    [InlineData("var params = JSON.parse('{\"pkgPath\":\"pkgs\\/P1234.pkg\"}');", "pkgs/P1234.pkg")]
    [InlineData("<a href=\"pkgs//P9876abc.pkg\">P9876abc.pkg</a>", "pkgs//P9876abc.pkg")]
    [InlineData("<p>none</p>", null)]
    public void Finds_package_path(string html, string? expected) =>
        Assert.Equal(expected, RokuDevServerParser.FindPackagePath(html));
}

public class RokuDevServerClientTests
{
    private const string Realm = "rokudev";
    private static readonly RokuDevServerTarget Target = new("10.0.0.5", 80, "rokudev", "secret");

    /// <summary>A fake Roku developer web server that checks Digest auth like the real one.</summary>
    private sealed class FakeRoku(string password, Func<HttpRequestMessage, string, HttpResponseMessage> respond) : HttpMessageHandler
    {
        private int _nonce;

        public List<string> Requests { get; } = [];

        public List<string> Bodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(ct);
            var authorized = request.Headers.Authorization is { Scheme: "Digest", Parameter: { } parameter } && Verify(request, parameter);
            Requests.Add($"{request.Method} {request.RequestUri!.PathAndQuery}{(authorized ? " (auth)" : string.Empty)}");
            if (!authorized)
            {
                var challenge = new HttpResponseMessage(HttpStatusCode.Unauthorized);
                challenge.Headers.WwwAuthenticate.Add(new AuthenticationHeaderValue(
                    "Digest", $"qop=\"auth\", realm=\"{Realm}\", nonce=\"{++_nonce}abc\""));
                return challenge;
            }

            Bodies.Add(body);
            return respond(request, body);
        }

        private bool Verify(HttpRequestMessage request, string parameter)
        {
            var sent = HttpDigest.ParseParameters(parameter);
            var expected = HttpDigest.CreateAuthorization(
                HttpDigest.ParseParameters($"realm=\"{Realm}\", nonce=\"{sent["nonce"]}\", qop=\"auth\""),
                request.Method.Method,
                request.RequestUri!.PathAndQuery,
                "rokudev",
                password,
                sent["cnonce"]);
            return HttpDigest.ParseParameters(expected)["response"] == sent["response"];
        }
    }

    private static HttpResponseMessage Html(string html) => new(HttpStatusCode.OK) { Content = new StringContent(html) };

    private static RokuDevServerClient Client(FakeRoku roku) => new(new HttpClient(roku), TimeProvider.System);

    [Fact]
    public async Task Install_probes_for_the_challenge_then_uploads_once_with_digest_auth()
    {
        var roku = new FakeRoku("secret", (_, _) => Html(
            "Shell.create('Roku.Message').trigger('Set message type', 'success').trigger('Set message content', 'Install Success.')"));

        var result = await Client(roku).InstallAsync(Target, [(byte)'P', (byte)'K', 3, 4], "my channel.zip");

        Assert.Equal(["GET /plugin_install", "POST /plugin_install (auth)"], roku.Requests);
        var body = Assert.Single(roku.Bodies);
        Assert.Contains("name=\"mysubmit\"", body);
        Assert.Contains("Replace", body);
        Assert.Contains("name=\"archive\"; filename=\"my channel.zip\"", body);
        Assert.Equal(new RokuDevMessageDto("success", "Install Success."), Assert.Single(result.Messages));
    }

    [Fact]
    public async Task Wrong_password_is_rejected_with_a_hint()
    {
        var roku = new FakeRoku("other", (_, _) => Html(string.Empty));

        var ex = await Assert.ThrowsAsync<RokuDevPasswordRejectedException>(() => Client(roku).VerifyPasswordAsync(Target));

        Assert.Contains("rejected the developer password", ex.Message);
        Assert.Equal(["GET /plugin_install", "GET /plugin_install", "GET /plugin_install"], roku.Requests);
    }

    [Fact]
    public async Task Install_reports_compile_failures()
    {
        var roku = new FakeRoku("secret", (_, _) => Html(
            "Shell.create('Roku.Message').trigger('Set message type', 'error').trigger('Set message content', 'Install Failure: Compilation Failed.')"));

        var ex = await Assert.ThrowsAsync<RokuRequestRejectedException>(() =>
            Client(roku).InstallAsync(Target, [(byte)'P', (byte)'K', 3, 4], "c.zip"));

        Assert.Equal("Install Failure: Compilation Failed.", ex.Message);
    }

    [Fact]
    public async Task Screenshot_downloads_the_linked_image()
    {
        var roku = new FakeRoku("secret", (request, _) => request.Method == HttpMethod.Post
            ? Html("<img src=\"pkgs/dev.jpg?time=42\">")
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([0xFF, 0xD8, 0xFF]) });

        var file = await Client(roku).ScreenshotAsync(Target);

        Assert.Equal("image/jpeg", file.ContentType);
        Assert.Equal([0xFF, 0xD8, 0xFF], file.Content);
        Assert.EndsWith(".jpg", file.FileName);
        Assert.Contains("GET /pkgs/dev.jpg?time=42 (auth)", roku.Requests);
        Assert.Contains("Screenshot", roku.Bodies[0]);
    }

    [Fact]
    public async Task Package_sends_name_version_and_signing_password_then_downloads()
    {
        var roku = new FakeRoku("secret", (request, _) => request.Method == HttpMethod.Post
            ? Html("<a href=\"pkgs//P1a2b.pkg\">P1a2b.pkg</a>")
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) });

        var file = await Client(roku).PackageAsync(Target, "My App", "1.2", "signing-pw");

        Assert.Equal("My App_1.2.pkg", file.FileName);
        Assert.Equal([1, 2, 3], file.Content);
        Assert.Contains("My App/1.2", roku.Bodies[0]);
        Assert.Contains("signing-pw", roku.Bodies[0]);
        Assert.Contains("GET /pkgs//P1a2b.pkg (auth)", roku.Requests);
    }

    [Fact]
    public async Task Rekey_requires_the_success_status()
    {
        var roku = new FakeRoku("secret", (_, _) => Html("<font color=\"red\">Failed: Invalid Password.</font>"));

        var ex = await Assert.ThrowsAsync<RokuRequestRejectedException>(() =>
            Client(roku).RekeyAsync(Target, [1, 2, 3], "old.pkg", "wrong"));

        Assert.Equal("Failed: Invalid Password.", ex.Message);
    }

    [Fact]
    public async Task Unreachable_dev_server_mentions_developer_mode()
    {
        var client = new RokuDevServerClient(new HttpClient(new ThrowingHandler()), TimeProvider.System);

        var ex = await Assert.ThrowsAsync<RokuUnreachableException>(() => client.DeleteAsync(Target));

        Assert.Contains("developer mode", ex.Message);
    }

    [Fact]
    public void Form_uses_an_unquoted_boundary_and_no_part_content_type_for_text()
    {
        using var form = RokuDevServerClient.BuildForm([new FormField("mysubmit", "Delete")]);

        Assert.DoesNotContain("\"", form.Headers.ContentType!.ToString());
        var part = Assert.Single(form);
        Assert.Null(part.Headers.ContentType);
        Assert.Equal("form-data; name=\"mysubmit\"", part.Headers.ContentDisposition!.ToString());
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            throw new HttpRequestException("Connection refused");
    }
}

public class RokuDevChannelServiceTests
{
    private static RokuDevChannelService Service(RokuDevice device, out IRokuDevPasswordProtector protector)
    {
        protector = new RokuDevPasswordProtector(new EphemeralDataProtectionProvider());
        return new RokuDevChannelService(
            new SingleRokuRepository(device),
            new RokuEcpClient(new HttpClient()),
            new RokuDevServerClient(new HttpClient(), TimeProvider.System),
            protector,
            Options.Create(new RokuOptions()));
    }

    [Fact]
    public async Task Actions_need_a_saved_password()
    {
        var service = Service(new RokuDevice { Id = 1, FriendlyName = "Den", IpAddress = "10.0.0.5" }, out _);

        var ex = await Assert.ThrowsAsync<ServiceValidationException>(() => service.DeleteAsync(1));

        Assert.Contains("Save the developer password for Den", ex.Message);
    }

    [Fact]
    public async Task Install_rejects_files_that_are_not_zips()
    {
        var device = new RokuDevice { Id = 1, FriendlyName = "Den", IpAddress = "10.0.0.5" };
        var service = Service(device, out var protector);
        device.DevPasswordProtected = protector.Protect("secret");

        var ex = await Assert.ThrowsAsync<ServiceValidationException>(() => service.InstallAsync(1, [1, 2, 3, 4], "x.zip"));

        Assert.Contains("not a zip", ex.Message);
    }

    [Fact]
    public async Task Unknown_roku_is_not_found()
    {
        var service = Service(new RokuDevice { Id = 1, IpAddress = "10.0.0.5" }, out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.ClearDevPasswordAsync(2));
    }

    [Fact]
    public void Password_protector_round_trips_and_survives_foreign_ciphertext()
    {
        var protector = new RokuDevPasswordProtector(new EphemeralDataProtectionProvider());

        Assert.Equal("secret", protector.Unprotect(protector.Protect("secret")));
        Assert.Null(protector.Unprotect("not-ciphertext"));
    }
}

public class RokuEcpDeveloperTests
{
    [Fact]
    public void Parses_apps()
    {
        const string xml = """
            <apps>
              <app id="31012" type="appl" version="1.2.3">FandangoNOW</app>
              <app id="dev" type="appl" version="1.0.1">My Dev Channel</app>
            </apps>
            """;

        var apps = RokuEcpClient.ParseApps(xml);

        Assert.Equal(new RokuAppDto("dev", "My Dev Channel", "1.0.1"), apps[1]);
        Assert.Equal(2, apps.Count);
    }

    [Fact]
    public void Device_info_reports_developer_mode_and_key()
    {
        const string xml = """
            <device-info>
              <serial-number>X1</serial-number>
              <developer-enabled>true</developer-enabled>
              <keyed-developer-id>abc123</keyed-developer-id>
            </device-info>
            """;

        var info = RokuDeviceInfo.Parse(xml);

        Assert.True(info.DeveloperEnabled);
        Assert.Equal("abc123", info.KeyedDeveloperId);
    }
}

public class RokuDebugConsoleManagerTests
{
    private sealed class RecordingNotifier : IRokuConsoleNotifier
    {
        public StringBuilder Output { get; } = new();

        public List<string> States { get; } = [];

        public Task OutputAsync(RokuConsoleOutputDto output)
        {
            lock (Output)
            {
                Output.Append(output.Text);
            }

            return Task.CompletedTask;
        }

        public Task StatusAsync(RokuConsoleStatusDto status)
        {
            lock (States)
            {
                States.Add(status.State);
            }

            return Task.CompletedTask;
        }
    }

    [Fact]
    public void Strips_telnet_negotiation()
    {
        byte[] data = [(byte)'a', 0xFF, 0xFB, 0x01, (byte)'b', 0xFF, 0xF1, (byte)'c'];

        Assert.Equal("abc"u8.ToArray(), RokuDebugConsoleManager.StripTelnet(data));
    }

    [Fact]
    public async Task Shares_one_connection_relays_output_and_input_and_closes_when_nobody_watches()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var services = new ServiceCollection()
            .AddScoped<IRokuDeviceRepository>(_ => new SingleRokuRepository(new RokuDevice { Id = 7, IpAddress = "127.0.0.1" }))
            .BuildServiceProvider();
        var notifier = new RecordingNotifier();
        using var manager = new RokuDebugConsoleManager(
            services.GetRequiredService<IServiceScopeFactory>(),
            notifier,
            Options.Create(new RokuOptions { DebugConsolePort = port }),
            NullLogger<RokuDebugConsoleManager>.Instance);

        await manager.SubscribeAsync(7, "viewer-a");
        await manager.SubscribeAsync(7, "viewer-b");
        using var roku = await listener.AcceptTcpClientAsync().WaitAsync(TimeSpan.FromSeconds(10));
        var stream = roku.GetStream();

        await stream.WriteAsync("------ Running dev 'My App' main ------\r\n"u8.ToArray());
        await WaitUntil(() => { lock (notifier.Output) { return notifier.Output.ToString().Contains("Running dev"); } });

        var snapshot = await manager.SubscribeAsync(7, "viewer-c");
        Assert.Contains("Running dev 'My App'", snapshot.Backlog);
        Assert.Equal("Connected", snapshot.Status.State);
        Assert.False(listener.Pending());

        await manager.SendAsync(7, "bt");
        var buffer = new byte[16];
        var read = await stream.ReadAsync(buffer).AsTask().WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal("bt\r\n", Encoding.ASCII.GetString(buffer, 0, read));

        manager.Unsubscribe(7, "viewer-a");
        manager.UnsubscribeAll("viewer-b");
        manager.Unsubscribe(7, "viewer-c");
        var closed = await stream.ReadAsync(buffer).AsTask().WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(0, closed);
        await Assert.ThrowsAsync<RokuRequestRejectedException>(() => manager.SendAsync(7, "cont"));
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "Timed out waiting for console output.");
            await Task.Delay(20);
        }
    }
}

internal sealed class SingleRokuRepository(RokuDevice device) : IRokuDeviceRepository
{
    public Task<IReadOnlyList<RokuDevice>> ListAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<RokuDevice>>([device]);

    public Task<RokuDevice?> GetAsync(int id, CancellationToken ct = default) => Task.FromResult(id == device.Id ? device : null);

    public Task<RokuDevice?> GetBySerialAsync(string serialNumber, CancellationToken ct = default) => Task.FromResult<RokuDevice?>(null);

    public Task<RokuDevice> UpsertBySerialAsync(RokuDevice value, CancellationToken ct = default) => Task.FromResult(value);

    public Task<bool> SetDevPasswordAsync(int id, string? protectedPassword, CancellationToken ct = default)
    {
        device.DevPasswordProtected = protectedPassword;
        return Task.FromResult(id == device.Id);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken ct = default) => Task.FromResult(false);
}
