using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SixBench.Services.Roku;

/// <summary>
/// One JSON message on the ECP-2 websocket (<c>ws://{roku}:8060/ecp-session</c>, subprotocol <c>ecp-2</c>).
/// The Roku sends <c>notify</c> messages on its own and a <c>response</c> to each request.
/// </summary>
/// <param name="Notify">Event name (for example <c>authenticate</c> or <c>textedit-opened</c>), or null.</param>
/// <param name="Response">Name of the request this answers, or null.</param>
/// <param name="Status">HTTP-style status of a response (<c>200</c> when it worked).</param>
/// <param name="Challenge">The <c>param-challenge</c> of an <c>authenticate</c> notification.</param>
/// <param name="Content">The decoded <c>content-data</c> of a response, if it had any.</param>
public sealed record RokuEcp2Message(string? Notify, string? Response, string? Status, string? Challenge, string? Content)
{
    /// <summary>Whether this is a successful response.</summary>
    public bool Ok => Status == "200";
}

/// <summary>
/// The parts of Roku's ECP-2 websocket protocol SixBench uses. It is what Roku's mobile app speaks; Roku does not
/// document it, so everything here is defensive about missing fields.
/// </summary>
public static class RokuEcp2Protocol
{
    /// <summary>Websocket path on the ECP port.</summary>
    public const string Path = "ecp-session";

    /// <summary>Websocket subprotocol.</summary>
    public const string SubProtocol = "ecp-2";

    /// <summary>Keyboard events: a text field gained focus, its text changed, or it closed.</summary>
    public static readonly IReadOnlyList<string> TextEditEvents = ["textedit-opened", "textedit-changed", "textedit-closed"];

    /// <summary>
    /// Answers the Roku's <c>authenticate</c> challenge: base64(SHA-1(challenge + key')), where key' is the shared key
    /// with each hex digit d replaced by (8 - d) mod 16.
    /// </summary>
    /// <param name="challenge">The <c>param-challenge</c> value, as sent.</param>
    /// <param name="key">The shared key (<see cref="Common.Options.RokuOptions.EcpSessionKey"/>).</param>
    /// <returns>The <c>param-response</c> value.</returns>
    public static string AuthResponse(string challenge, string key)
    {
        var transformed = string.Concat(key.Select(c =>
            Uri.IsHexDigit(c) ? "0123456789ABCDEF"[(24 - Convert.ToInt32(c.ToString(), 16)) & 15] : c));
        return Convert.ToBase64String(SHA1.HashData(Encoding.UTF8.GetBytes(challenge + transformed)));
    }

    /// <summary>Builds a request message.</summary>
    /// <param name="request">Request name, for example <c>query-textedit-state</c>.</param>
    /// <param name="requestId">Id the Roku echoes back as <c>response-id</c>.</param>
    /// <param name="parameters">Parameters, sent as <c>param-{name}</c>.</param>
    /// <returns>The JSON text.</returns>
    public static string Request(string request, long requestId, IReadOnlyDictionary<string, string>? parameters = null)
    {
        var json = new JsonObject
        {
            ["request"] = request,
            ["request-id"] = requestId.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };
        foreach (var (name, value) in parameters ?? new Dictionary<string, string>())
        {
            json[$"param-{name}"] = value;
        }

        return json.ToJsonString();
    }

    /// <summary>The authentication request.</summary>
    /// <param name="requestId">Request id.</param>
    /// <param name="challenge">The Roku's challenge.</param>
    /// <param name="key">The shared key.</param>
    /// <returns>The JSON text.</returns>
    public static string Authenticate(long requestId, string challenge, string key) =>
        Request("authenticate", requestId, new Dictionary<string, string> { ["response"] = AuthResponse(challenge, key) });

    /// <summary>Subscribes to the keyboard events.</summary>
    /// <param name="requestId">Request id.</param>
    /// <returns>The JSON text.</returns>
    public static string RequestTextEditEvents(long requestId) =>
        Request("request-events", requestId, new Dictionary<string, string> { ["events"] = string.Join(',', TextEditEvents.Select(e => "+" + e)) });

    /// <summary>Asks whether a text field is being edited.</summary>
    /// <param name="requestId">Request id.</param>
    /// <returns>The JSON text.</returns>
    public static string QueryTextEditState(long requestId) => Request("query-textedit-state", requestId);

    /// <summary>Parses a message from the Roku.</summary>
    /// <param name="json">The message.</param>
    /// <returns>The message, or null when it is not a JSON object.</returns>
    public static RokuEcp2Message? Parse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            string? content = null;
            if (String(root, "content-data") is { } data)
            {
                try
                {
                    content = Encoding.UTF8.GetString(Convert.FromBase64String(data));
                }
                catch (FormatException)
                {
                    content = data;
                }
            }

            return new RokuEcp2Message(String(root, "notify"), String(root, "response"), String(root, "status"), String(root, "param-challenge"), content);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Reads a <c>query-textedit-state</c> result, for example <c>{"textedit-state":{"textedit-id":"none"}}</c>.
    /// </summary>
    /// <param name="content">The decoded response content.</param>
    /// <returns>The field being edited (null id when none), or null when the content is not a textedit state.</returns>
    public static (string? TextEditId, string? Text)? ParseTextEditState(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.ValueKind != JsonValueKind.Object
                || !doc.RootElement.TryGetProperty("textedit-state", out var state)
                || state.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var id = String(state, "textedit-id");
            if (string.IsNullOrWhiteSpace(id) || id.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                return (null, null);
            }

            return (id, String(state, "text"));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? String(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value)
            ? value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => value.GetRawText(),
                _ => null,
            }
            : null;
}
