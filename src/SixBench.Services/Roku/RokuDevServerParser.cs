using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SixBench.Common.Dtos;

namespace SixBench.Services.Roku;

/// <summary>
/// Reads the HTML pages the Roku developer web server answers with. The format has changed across Roku OS
/// versions, so this follows the patterns the RokuCommunity <c>roku-deploy</c> tool relies on.
/// </summary>
public static partial class RokuDevServerParser
{
    /// <summary>Message type for errors.</summary>
    public const string Error = "error";

    /// <summary>Message type for information.</summary>
    public const string Info = "info";

    /// <summary>Message type for success.</summary>
    public const string Success = "success";

    /// <summary>
    /// Extracts the message banners from a response page.
    /// </summary>
    /// <param name="html">Response body.</param>
    /// <returns>Messages in page order, without duplicates.</returns>
    public static IReadOnlyList<RokuDevMessageDto> ParseMessages(string html)
    {
        var messages = new List<RokuDevMessageDto>();

        // Older pages: Shell.create('Roku.Message').trigger('set message type', 'error').trigger('set message content', '...')
        foreach (Match match in ShellMessageRegex().Matches(html))
        {
            Add(messages, match.Groups[1].Value, UnescapeJsString(match.Groups[2].Value));
        }

        // Newer pages: var params = JSON.parse('{"messages":[{"text":"...","text_type":"text","type":"error"}]}');
        foreach (Match match in JsonParseRegex().Matches(html))
        {
            foreach (var (type, text) in ReadJsonMessages(UnescapeJsString(match.Groups[1].Value)))
            {
                Add(messages, type, text);
            }
        }

        // Packager and Rekey report in a red <font> tag ("Success." or "Failed: ...").
        foreach (Match match in FontMessageRegex().Matches(html))
        {
            var text = WebUtility.HtmlDecode(match.Groups[1].Value).Trim();
            var type = text.StartsWith("Success", StringComparison.OrdinalIgnoreCase) ? Success
                : text.StartsWith("Fail", StringComparison.OrdinalIgnoreCase) || text.Contains("error", StringComparison.OrdinalIgnoreCase) ? Error
                : Info;
            Add(messages, type, text);
        }

        return messages;
    }

    /// <summary>
    /// Finds the screenshot link (<c>pkgs/dev.jpg?time=...</c> or <c>.png</c>) in a Screenshot response.
    /// </summary>
    /// <param name="html">Response body.</param>
    /// <returns>The path relative to the server root, or null.</returns>
    public static string? FindScreenshotPath(string html) =>
        ScreenshotRegex().Match(html) is { Success: true } match ? WebUtility.HtmlDecode(match.Groups[1].Value) : null;

    /// <summary>
    /// Finds the signed package link in a Package response.
    /// </summary>
    /// <param name="html">Response body.</param>
    /// <returns>The path relative to the server root, or null.</returns>
    public static string? FindPackagePath(string html)
    {
        if (PackageJsonRegex().Match(html) is { Success: true } json)
        {
            return UnescapeJsString(json.Groups[1].Value).TrimStart('/');
        }

        return PackageLinkRegex().Match(html) is { Success: true } link ? WebUtility.HtmlDecode(link.Groups[1].Value) : null;
    }

    /// <summary>
    /// Text of the red <c>&lt;font&gt;</c> status (Packager and Rekey), if present.
    /// </summary>
    /// <param name="html">Response body.</param>
    /// <returns>The status text, or null.</returns>
    public static string? FindFontStatus(string html) =>
        FontMessageRegex().Match(html) is { Success: true } match ? WebUtility.HtmlDecode(match.Groups[1].Value).Trim() : null;

    /// <summary>
    /// Undoes JavaScript string-literal escapes (<c>\'</c>, <c>\\</c>, <c>\n</c>, <c>\uXXXX</c>, ...).
    /// </summary>
    /// <param name="value">The literal's contents, without the quotes.</param>
    /// <returns>The string value.</returns>
    public static string UnescapeJsString(string value)
    {
        if (!value.Contains('\\', StringComparison.Ordinal))
        {
            return value;
        }

        var sb = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c != '\\' || i + 1 >= value.Length)
            {
                sb.Append(c);
                continue;
            }

            var next = value[++i];
            switch (next)
            {
                case 'n': sb.Append('\n'); break;
                case 'r': sb.Append('\r'); break;
                case 't': sb.Append('\t'); break;
                case 'b': sb.Append('\b'); break;
                case 'f': sb.Append('\f'); break;
                case 'u' when i + 4 < value.Length
                    && int.TryParse(value.AsSpan(i + 1, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code):
                    sb.Append((char)code);
                    i += 4;
                    break;
                case 'x' when i + 2 < value.Length
                    && int.TryParse(value.AsSpan(i + 1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex):
                    sb.Append((char)hex);
                    i += 2;
                    break;
                default:
                    // \' \" \\ \/ and any other escaped character stand for themselves.
                    sb.Append(next);
                    break;
            }
        }

        return sb.ToString();
    }

    private static IEnumerable<(string Type, string Text)> ReadJsonMessages(string json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
        }
        catch (JsonException)
        {
            yield break;
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("messages", out var list)
                || list.ValueKind != JsonValueKind.Array)
            {
                yield break;
            }

            foreach (var item in list.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object
                    && item.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String
                    && item.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String
                    && (!item.TryGetProperty("text_type", out var textType) || textType.GetString() == "text"))
                {
                    yield return (type.GetString()!, text.GetString()!);
                }
            }
        }
    }

    private static void Add(List<RokuDevMessageDto> messages, string type, string text)
    {
        text = text.Trim();
        if (text.Length == 0)
        {
            return;
        }

        var normalized = type.ToLowerInvariant() switch
        {
            "error" or "err" or "failure" => Error,
            "success" => Success,
            _ => Info,
        };
        var message = new RokuDevMessageDto(normalized, text);
        if (!messages.Contains(message))
        {
            messages.Add(message);
        }
    }

    [GeneratedRegex(@"Shell\.create\('Roku\.Message'\)\.trigger\('[\w\s]+',\s*'(\w+)'\)\.trigger\('[\w\s]+',\s*'(.*?)'\)", RegexOptions.IgnoreCase)]
    private static partial Regex ShellMessageRegex();

    [GeneratedRegex(@"JSON\.parse\('(.+)'\);", RegexOptions.IgnoreCase)]
    private static partial Regex JsonParseRegex();

    [GeneratedRegex(@"<font color=""red"">([^<]+)</font>", RegexOptions.IgnoreCase)]
    private static partial Regex FontMessageRegex();

    [GeneratedRegex(@"[""'](pkgs/dev\.(?:jpg|png)\?[^""']+)[""']", RegexOptions.IgnoreCase)]
    private static partial Regex ScreenshotRegex();

    [GeneratedRegex(@"""pkgPath""\s*:\s*""(.*?)""", RegexOptions.IgnoreCase)]
    private static partial Regex PackageJsonRegex();

    [GeneratedRegex(@"<a href=""(pkgs/[^""]+\.pkg)"">", RegexOptions.IgnoreCase)]
    private static partial Regex PackageLinkRegex();
}
