using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SixBench.Common.Enums;
using SixBench.Common.Utilities;
using SixBench.Services.Capture;
using SixBench.Services.Ffmpeg;

namespace SixBench.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class ApiTests(ApiTests.Factory factory) : IClassFixture<ApiTests.Factory>, IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync() => _client = await factory.SignInAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Requires_sign_in_except_health()
    {
        var anonymous = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/roku-devices")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/health")).StatusCode);
    }

    [Fact]
    public async Task Health_is_ok()
    {
        var health = await _client.GetFromJsonAsync<JsonElement>("/health");

        Assert.Equal("Healthy", health.GetProperty("status").GetString());
        Assert.Equal("Healthy", health.GetProperty("checks").GetProperty("ffmpeg").GetProperty("status").GetString());
    }

    [Fact]
    public async Task Lists_capture_devices_with_url_safe_ids()
    {
        var devices = await _client.GetFromJsonAsync<JsonElement>("/api/v1/capture-devices");

        var device = Assert.Single(devices.EnumerateArray());
        Assert.Equal("USB Video", device.GetProperty("name").GetString());
        Assert.Equal(DeviceKey.Encode("test:usb"), device.GetProperty("id").GetString());
        Assert.Equal("USB Digital Audio", device.GetProperty("audioInput").GetString());
    }

    [Fact]
    public async Task Link_round_trip_and_validation()
    {
        var id = DeviceKey.Encode("test:usb");

        var bad = await _client.PutAsJsonAsync($"/api/v1/encoder-links/{id}", new { displayName = "", videoSize = "huge" });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);

        var missingRoku = await _client.PutAsJsonAsync($"/api/v1/encoder-links/{id}", new { displayName = "Den", rokuDeviceId = 999 });
        Assert.Equal(HttpStatusCode.BadRequest, missingRoku.StatusCode);

        var ok = await _client.PutAsJsonAsync($"/api/v1/encoder-links/{id}", new { displayName = "Den", allowDeviceAudio = true, frameRate = 60 });
        ok.EnsureSuccessStatusCode();
        var link = await ok.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("USB Video", link.GetProperty("videoInput").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/v1/encoder-links/{id}")).StatusCode);
    }

    [Fact]
    public async Task Errors_are_problem_details()
    {
        var notFound = await _client.GetAsync("/api/v1/roku-devices/12345");
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
        Assert.Equal("application/problem+json", notFound.Content.Headers.ContentType?.MediaType);

        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync("/api/v1/capture-devices/!!!")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/v1/does-not-exist")).StatusCode);
    }

    [Fact]
    public async Task Dev_tools_validate_and_gate_the_registry()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/v1/roku-devices/999/dev-tools/sgnodes")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/v1/roku-devices/999/dev-tools/chanperf")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync("/api/v1/roku-devices/999/dev-tools/sgnodes?scope=Nodes")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync("/api/v1/roku-devices/999/dev-tools/registry/a.b")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/v1/roku-devices/999/dev-tools/registry/dev")).StatusCode);

        var created = await _client.PostAsJsonAsync("/api/v1/users", new { userName = "dana", password = "Dana-pass-1!", role = "User" });
        created.EnsureSuccessStatusCode();
        var user = await factory.SignInAsync("dana", "Dana-pass-1!");
        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/api/v1/roku-devices/999/dev-tools/registry/dev")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await user.GetAsync("/api/v1/roku-devices/999/dev-tools/chanperf")).StatusCode);
    }

    [Fact]
    public async Task Stream_endpoint_requires_websocket_and_is_hidden_from_swagger()
    {
        var response = await _client.GetAsync($"/api/v1/streams/{DeviceKey.Encode("test:usb")}/ws");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var swagger = await _client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        var paths = swagger.GetProperty("paths").EnumerateObject().Select(p => p.Name).ToList();
        Assert.Contains("/api/v1/roku-devices/{id}/keys", paths);
        Assert.Contains("/api/v1/roku-devices/{id}/dev-tools/sgnodes", paths);
        Assert.DoesNotContain(paths, p => p.EndsWith("/ws", StringComparison.Ordinal));
    }

    public sealed class Factory : WebApplicationFactory<Program>
    {
        private readonly string _root = Directory.CreateTempSubdirectory("sixbench-api-").FullName;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:SixBench", $"Data Source={Path.Combine(_root, "test.db")}");
            TestAuth.Configure(builder, _root);
            builder.UseSetting("Serilog:WriteTo:1:Name", "Console");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICaptureDeviceEnumerator>();
                services.AddSingleton<ICaptureDeviceEnumerator, FakeEnumerator>();
                services.RemoveAll<IFfmpegCapabilities>();
                services.AddSingleton<IFfmpegCapabilities, FakeCapabilities>();
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            Directory.Delete(_root, recursive: true);
        }
    }

    private sealed class FakeEnumerator : ICaptureDeviceEnumerator
    {
        public HostPlatform Platform => HostPlatform.MacOS;

        public Task<DeviceInventory> EnumerateAsync(CancellationToken ct = default) => Task.FromResult(new DeviceInventory(
            Platform,
            [new VideoDeviceInfo("test:usb", "USB Video", "USB Video")],
            [new AudioDeviceInfo("MacBook Pro Microphone", "MacBook Pro Microphone"), new AudioDeviceInfo("USB Digital Audio", "USB Digital Audio")]));
    }

    private sealed class FakeCapabilities : IFfmpegCapabilities
    {
        public Task<FfmpegCapabilityReport> GetAsync(CancellationToken ct = default) =>
            Task.FromResult(new FfmpegCapabilityReport(true, "libx264", true, true, ["libx264"], null));
    }
}
