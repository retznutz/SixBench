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

namespace SixBench.Tests.Api;

public sealed class ApiTests : IClassFixture<ApiTests.Factory>
{
    private readonly HttpClient _client;

    public ApiTests(Factory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Health_is_ok() => Assert.Equal("Healthy", await _client.GetStringAsync("/health"));

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
    public async Task Stream_endpoint_requires_websocket_and_is_hidden_from_swagger()
    {
        var response = await _client.GetAsync($"/api/v1/streams/{DeviceKey.Encode("test:usb")}/ws");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var swagger = await _client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        var paths = swagger.GetProperty("paths").EnumerateObject().Select(p => p.Name).ToList();
        Assert.Contains("/api/v1/roku-devices/{id}/keys", paths);
        Assert.DoesNotContain(paths, p => p.EndsWith("/ws", StringComparison.Ordinal));
    }

    public sealed class Factory : WebApplicationFactory<Program>
    {
        private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"sixbench-api-{Guid.NewGuid():N}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:SixBench", $"Data Source={_dbPath}");
            builder.UseSetting("Serilog:WriteTo:1:Name", "Console");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICaptureDeviceEnumerator>();
                services.AddSingleton<ICaptureDeviceEnumerator, FakeEnumerator>();
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            File.Delete(_dbPath);
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
}
