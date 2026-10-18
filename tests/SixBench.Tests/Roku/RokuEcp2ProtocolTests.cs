using System.Text;
using System.Text.Json;
using SixBench.Common.Options;
using SixBench.Services.Roku;

namespace SixBench.Tests.Roku;

public class RokuEcp2ProtocolTests
{
    private static readonly string Key = new RokuOptions().EcpSessionKey;

    [Fact]
    public void Answers_the_authentication_challenge()
    {
        // Captured from a Roku Express 4K+ on Roku OS 16.0.4, which accepted this response.
        Assert.Equal("f7SQhIduX1FTjVKE/gjbcPqMDNQ=", RokuEcp2Protocol.AuthResponse("A1uSj72PfLdjCoQlQkKotA==", Key));
    }

    [Fact]
    public void Builds_requests_with_string_ids_and_param_prefixes()
    {
        using var auth = JsonDocument.Parse(RokuEcp2Protocol.Authenticate(1, "A1uSj72PfLdjCoQlQkKotA==", Key));
        Assert.Equal("authenticate", auth.RootElement.GetProperty("request").GetString());
        Assert.Equal("1", auth.RootElement.GetProperty("request-id").GetString());
        Assert.Equal("f7SQhIduX1FTjVKE/gjbcPqMDNQ=", auth.RootElement.GetProperty("param-response").GetString());

        using var events = JsonDocument.Parse(RokuEcp2Protocol.RequestTextEditEvents(2));
        Assert.Equal("request-events", events.RootElement.GetProperty("request").GetString());
        Assert.Equal("+textedit-opened,+textedit-changed,+textedit-closed", events.RootElement.GetProperty("param-events").GetString());

        Assert.Equal("""{"request":"query-textedit-state","request-id":"3"}""", RokuEcp2Protocol.QueryTextEditState(3));
    }

    [Fact]
    public void Parses_the_challenge()
    {
        var message = RokuEcp2Protocol.Parse(
            """{"notify":"authenticate","param-challenge":"A1uSj72PfLdjCoQlQkKotA==","param-methods":["client-id","jwt"],"timestamp":"26541.026"}""");

        Assert.NotNull(message);
        Assert.Equal("authenticate", message.Notify);
        Assert.Equal("A1uSj72PfLdjCoQlQkKotA==", message.Challenge);
        Assert.Null(message.Response);
    }

    [Fact]
    public void Decodes_response_content()
    {
        var content = """{"textedit-state":{"textedit-id":"none"}}""";
        var message = RokuEcp2Protocol.Parse(
            $$"""{"content-data":"{{Convert.ToBase64String(Encoding.UTF8.GetBytes(content))}}","content-type":"application/json; charset=\"utf-8\"","response":"query-textedit-state","response-id":"2","status":"200","status-msg":"OK"}""");

        Assert.NotNull(message);
        Assert.True(message.Ok);
        Assert.Equal("query-textedit-state", message.Response);
        Assert.Equal(content, message.Content);
    }

    [Fact]
    public void Reports_rejections()
    {
        var message = RokuEcp2Protocol.Parse("""{"response":"request-events","response-id":"2","status":"404","status-msg":"Not Found"}""");

        Assert.NotNull(message);
        Assert.False(message.Ok);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[1,2]")]
    public void Ignores_messages_that_are_not_objects(string json) => Assert.Null(RokuEcp2Protocol.Parse(json));

    [Theory]
    [InlineData("""{"textedit-state":{"textedit-id":"none"}}""")]
    [InlineData("""{"textedit-state":{"textedit-id":""}}""")]
    [InlineData("""{"textedit-state":{}}""")]
    public void No_text_field_means_closed(string content)
    {
        var state = RokuEcp2Protocol.ParseTextEditState(content);

        Assert.NotNull(state);
        Assert.Null(state.Value.TextEditId);
    }

    [Fact]
    public void A_text_field_id_means_open()
    {
        var state = RokuEcp2Protocol.ParseTextEditState("""{"textedit-state":{"textedit-id":"12","text":"star"}}""");

        Assert.NotNull(state);
        Assert.Equal("12", state.Value.TextEditId);
        Assert.Equal("star", state.Value.Text);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("<xml/>")]
    [InlineData("""{"other":1}""")]
    public void Unrecognised_content_is_not_a_state(string? content) => Assert.Null(RokuEcp2Protocol.ParseTextEditState(content));
}
