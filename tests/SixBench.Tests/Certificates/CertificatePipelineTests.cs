using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using SixBench.Api.Infrastructure;
using SixBench.Services.Certificates;

namespace SixBench.Tests.Certificates;

public sealed class CertificatePipelineTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("sixbench-cert-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void Certes_pfx_loads_with_private_key_and_reports_info()
    {
        var path = TestCertificates.WritePfx(_dir, "sixbench.example.com", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(89));

        var certificate = new ServerCertificate(NullLogger.Instance);
        Assert.True(certificate.Load(path));
        Assert.True(certificate.Current!.HasPrivateKey);

        var info = CertificateService.GetCurrentCertificateInfo(path)!;
        Assert.Equal("sixbench.example.com", info.Domain);
        Assert.Contains("Test Intermediate", info.Issuer, StringComparison.Ordinal);
        Assert.Null(CertificateService.GetCurrentCertificateInfo(Path.Combine(_dir, "missing.pfx")));
    }

    [Fact]
    public async Task Kestrel_serves_the_loaded_certificate_and_picks_up_a_reload()
    {
        var first = TestCertificates.WritePfx(_dir, "first.example.com", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(89), "a.pfx");
        var second = TestCertificates.WritePfx(_dir, "second.example.com", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(89), "b.pfx");
        var certificate = new ServerCertificate(NullLogger.Instance);
        certificate.Load(first);

        var port = FreePort();
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.ConfigureKestrel(k => k.Listen(IPAddress.Loopback, port, o => o.UseHttps(h => h.ServerCertificateSelector = (_, _) => certificate.Current)));
        await using var app = builder.Build();
        app.MapGet("/", () => "ok");
        await app.StartAsync();

        Assert.Equal("CN=first.example.com", await ServedSubjectAsync(port));
        certificate.Load(second);
        Assert.Equal("CN=second.example.com", await ServedSubjectAsync(port));
    }

    [Theory]
    [InlineData("/usr/local/share/dotnet/dotnet", "/app/SixBench.Api.dll|--urls|x", "/usr/local/share/dotnet/dotnet", "/app/SixBench.Api.dll|--urls|x")]
    [InlineData("C:/Program Files/dotnet/dotnet.exe", "C:/app/SixBench.Api.dll", "C:/Program Files/dotnet/dotnet.exe", "C:/app/SixBench.Api.dll")]
    [InlineData("/app/SixBench.Api", "/app/SixBench.Api.dll|--x", "/app/SixBench.Api", "--x")]
    [InlineData("C:/app/SixBench.Api.exe", "C:/app/SixBench.Api.dll", "C:/app/SixBench.Api.exe", "")]
    public void Relaunch_command_matches_how_the_app_was_started(string processPath, string args, string expectedFile, string expectedArgs)
    {
        var (file, arguments) = ServerRestarter.BuildRelaunchCommand(processPath, args.Split('|'));

        Assert.Equal(expectedFile, file);
        Assert.Equal(expectedArgs, string.Join('|', arguments));
    }

    private static async Task<string> ServedSubjectAsync(int port)
    {
        using var tcp = new TcpClient();
        await tcp.ConnectAsync(IPAddress.Loopback, port);
        await using var ssl = new SslStream(tcp.GetStream(), false, (_, _, _, _) => true);
        await ssl.AuthenticateAsClientAsync("localhost");
        return ssl.RemoteCertificate!.Subject;
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
