using System.Net;
using SixBench.Common.Dtos;
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

public class RokuDevToolsTests
{
    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(request.RequestUri!.PathAndQuery);
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }

    private const string SgNodes = """
        <?xml version="1.0" encoding="UTF-8" ?>
        <sgnodes>
          <All_Nodes>
            <Default children="0" focusable="false" focused="false" index="0" name="" opacity="100" thread="render" visible="true" />
            <MainScene _sn="1" bounds="{0, 0, 1920, 1080}" bscref="1" children="0" extends="Scene" focusable="true" focused="true" osref="3" rcid="0">
              <Poster _sn="2" bounds="{0, 0, 1920, 1080}" bscref="0" loadStatus="3" osref="2" rcid="0" uri="/RokuOS/background.png" />
            </MainScene>
            <Node _psn="1" _sn="9" bscref="1" osref="1" rcid="0" />
          </All_Nodes>
          <status>OK</status>
        </sgnodes>
        """;

    private const string ChanPerf = """
        <?xml version="1.0" encoding="UTF-8" ?>
        <chanperf>
          <timestamp>1672980398506</timestamp>
          <plugin>
            <cpu-percent><duration-seconds>1.000000</duration-seconds><user>12.2</user><sys>5.5</sys></cpu-percent>
            <memory><used>87785472</used><res>87785472</res><anon>24027136</anon><swap>0</swap><file>24727552</file><shared>39030784</shared></memory>
            <id>dev</id>
            <unsecured><process-id>6861</process-id></unsecured>
          </plugin>
          <status>OK</status>
        </chanperf>
        """;

    private const string Registry = """
        <?xml version="1.0" encoding="UTF-8" ?>
        <plugin-registry>
          <registry>
            <dev-id>e090ac01d342483bb28831a7e1afff8e</dev-id>
            <plugins>dev</plugins>
            <space-available>9168</space-available>
            <sections>
              <section>
                <name>UserInfo</name>
                <items>
                  <item><key>NextPaymentDate</key><value>2022-09-17T17:17:55</value></item>
                  <item><key>UserId</key><value>1429492</value></item>
                </items>
              </section>
            </sections>
          </registry>
          <status>OK</status>
        </plugin-registry>
        """;

    private static RokuDevToolsClient Client(StubHandler handler) => new(new HttpClient(handler), TimeProvider.System);

    [Fact]
    public void Parses_sgnodes_tree_with_attributes()
    {
        var result = RokuDevToolsParser.ParseSgNodes(SgNodes, SgNodeScope.All, DateTime.UnixEpoch);

        Assert.Equal(["Default", "MainScene", "Node"], result.Nodes.Select(n => n.Type));
        Assert.Equal(4, result.TotalNodes);
        var poster = Assert.Single(result.Nodes[1].Children);
        Assert.Equal("Poster", poster.Type);
        Assert.Equal("/RokuOS/background.png", poster.Attributes["uri"]);
        Assert.Equal("true", result.Nodes[1].Attributes["focused"]);
    }

    [Fact]
    public void Parses_sgnodes_without_a_section_wrapper()
    {
        const string unwrapped = """
            <sgnodes>
              <Group _sn="4" bscref="1" osref="0" rcid="0"><Label _sn="5" text="Hi" /></Group>
              <status>OK</status>
            </sgnodes>
            """;

        var result = RokuDevToolsParser.ParseSgNodes(unwrapped, SgNodeScope.Roots, DateTime.UnixEpoch);

        Assert.Equal("Group", Assert.Single(result.Nodes).Type);
        Assert.Equal(2, result.TotalNodes);
    }

    [Fact]
    public void Parses_chanperf()
    {
        var perf = RokuDevToolsParser.ParseChanPerf(ChanPerf);

        Assert.Equal("dev", perf.AppId);
        Assert.Equal(1672980398506, perf.TimestampMs);
        Assert.Equal(12.2, perf.CpuUserPercent);
        Assert.Equal(5.5, perf.CpuSysPercent);
        Assert.Equal(87785472, perf.MemoryUsedBytes);
        Assert.Equal(0, perf.MemorySwapBytes);
        Assert.Equal(6861, perf.ProcessId);
    }

    [Fact]
    public void Parses_registry_sections()
    {
        var registry = RokuDevToolsParser.ParseRegistry(Registry, "dev");

        Assert.Equal("e090ac01d342483bb28831a7e1afff8e", registry.DevId);
        Assert.Equal(9168, registry.SpaceAvailableBytes);
        var section = Assert.Single(registry.Sections);
        Assert.Equal("UserInfo", section.Name);
        Assert.Equal(new RokuRegistryItemDto("UserId", "1429492"), section.Items[1]);
    }

    [Fact]
    public void Failed_status_becomes_rejected_with_roku_error()
    {
        const string failed = "<chanperf><status>FAILED</status><error>No dev channel running</error></chanperf>";

        var ex = Assert.Throws<RokuRequestRejectedException>(() => RokuDevToolsParser.ParseChanPerf(failed));
        Assert.Contains("No dev channel running", ex.Message);
    }

    [Theory]
    [InlineData(SgNodeScope.All, null, false, "/query/sgnodes/all")]
    [InlineData(SgNodeScope.Roots, null, true, "/query/sgnodes/roots?sizes=true")]
    [InlineData(SgNodeScope.Nodes, "my list", true, "/query/sgnodes/nodes?node-id=my%20list&sizes=true")]
    public async Task Builds_sgnodes_urls(SgNodeScope scope, string? nodeId, bool sizes, string expected)
    {
        var handler = new StubHandler(HttpStatusCode.OK, SgNodes);

        await Client(handler).GetSgNodesAsync("10.0.0.5", 8060, scope, nodeId, sizes);

        Assert.Equal(expected, Assert.Single(handler.Requests));
    }

    [Fact]
    public async Task Forbidden_is_rejected_not_unreachable()
    {
        var handler = new StubHandler(HttpStatusCode.Forbidden, string.Empty);

        var ex = await Assert.ThrowsAsync<RokuRequestRejectedException>(() => Client(handler).GetChanPerfAsync("10.0.0.5", 8060));
        Assert.Contains("developer mode", ex.Message);
    }

    [Fact]
    public async Task Escapes_registry_app_id()
    {
        var handler = new StubHandler(HttpStatusCode.OK, Registry);

        await Client(handler).GetRegistryAsync("10.0.0.5", 8060, "12345_ab");

        Assert.Equal("/query/registry/12345_ab", Assert.Single(handler.Requests));
    }
}
