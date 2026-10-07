using System.Net;
using SixBench.Common.Enums;
using SixBench.Services.Exceptions;
using SixBench.Services.Roku;

namespace SixBench.Tests.Roku;

public class RokuEcpClientTests
{
    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add($"{request.Method} {request.RequestUri!.AbsoluteUri}");
            return Task.FromResult(respond(request));
        }
    }

    private const string DeviceInfo = """
        <?xml version="1.0" encoding="UTF-8" ?>
        <device-info>
          <serial-number>X00400ABCDEF</serial-number>
          <model-name>Roku Ultra</model-name>
          <friendly-device-name>Roku Ultra - X00400ABCDEF</friendly-device-name>
          <user-device-name>Living Room</user-device-name>
        </device-info>
        """;

    [Theory]
    [InlineData(KeyAction.Press, "keypress/Home")]
    [InlineData(KeyAction.Down, "keydown/Home")]
    [InlineData(KeyAction.Up, "keyup/Home")]
    public async Task Sends_key_actions(KeyAction action, string path)
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = new RokuEcpClient(new HttpClient(handler));

        await client.SendKeyAsync("10.0.0.5", 8060, RokuKey.Home, action);

        Assert.Equal($"POST http://10.0.0.5:8060/{path}", Assert.Single(handler.Requests));
    }

    [Theory]
    [InlineData("a", "Lit_a")]
    [InlineData(" ", "Lit_%20")]
    [InlineData("/", "Lit_%2F")]
    [InlineData("é", "Lit_%C3%A9")]
    [InlineData("😀", "Lit_%F0%9F%98%80")]
    public async Task Percent_encodes_literals(string character, string expected)
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = new RokuEcpClient(new HttpClient(handler));

        await client.SendLiteralAsync("10.0.0.5", 8060, character);

        Assert.EndsWith($"/keypress/{expected}", Assert.Single(handler.Requests));
    }

    [Fact]
    public async Task Parses_device_info_preferring_user_name()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(DeviceInfo) });
        var client = new RokuEcpClient(new HttpClient(handler));

        var info = await client.GetDeviceInfoAsync("10.0.0.5", 8060);

        Assert.Equal(new RokuDeviceInfo("X00400ABCDEF", "Living Room", "Roku Ultra"), info);
    }

    [Fact]
    public async Task Connection_failures_become_unreachable()
    {
        var handler = new RecordingHandler(_ => throw new HttpRequestException("Connection refused"));
        var client = new RokuEcpClient(new HttpClient(handler));

        var ex = await Assert.ThrowsAsync<RokuUnreachableException>(() => client.SendKeyAsync("10.0.0.9", 8060, RokuKey.Up, KeyAction.Press));
        Assert.IsType<HttpRequestException>(ex.InnerException);
    }

    [Fact]
    public async Task Forbidden_explains_the_mobile_apps_setting()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden));
        var client = new RokuEcpClient(new HttpClient(handler));

        var ex = await Assert.ThrowsAsync<RokuUnreachableException>(() => client.SendKeyAsync("10.0.0.9", 8060, RokuKey.Up, KeyAction.Press));
        Assert.Contains("Control by mobile apps", ex.Message);
    }
}

public class SsdpResponseParserTests
{
    [Fact]
    public void Extracts_location_from_roku_response()
    {
        const string response = "HTTP/1.1 200 OK\r\nCache-Control: max-age=3600\r\nST: roku:ecp\r\n" +
            "USN: uuid:roku:ecp:X00400ABCDEF\r\nExt: \r\nServer: Roku/12.5.0 UPnP/1.0 Roku/12.5.0\r\n" +
            "LOCATION: http://192.168.1.20:8060/\r\n\r\n";

        Assert.Equal(new Uri("http://192.168.1.20:8060/"), SsdpResponseParser.ParseLocation(response));
    }

    [Theory]
    [InlineData("HTTP/1.1 200 OK\r\nST: urn:dial-multiscreen-org:service:dial:1\r\nLOCATION: http://192.168.1.30:8008/\r\n\r\n")]
    [InlineData("NOTIFY * HTTP/1.1\r\nNT: roku:ecp\r\nLOCATION: http://192.168.1.20:8060/\r\n\r\n")]
    [InlineData("")]
    public void Ignores_non_roku_responses(string response) => Assert.Null(SsdpResponseParser.ParseLocation(response));
}
