using System.Text.Json;
using System.Text.RegularExpressions;

namespace SixBench.Services.Capture;

/// <summary>
/// Pure parsers for the device-listing output of ffmpeg and OS tools. Kept free of I/O so they can be unit tested.
/// </summary>
public static partial class DeviceListParsers
{
    /// <summary>
    /// Parses <c>ffmpeg -f avfoundation -list_devices true -i ""</c> stderr.
    /// "Capture screen" pseudo-devices are excluded.
    /// </summary>
    /// <param name="stderr">ffmpeg stderr.</param>
    /// <returns>Video device names and audio device names, in index order.</returns>
    public static (IReadOnlyList<string> Video, IReadOnlyList<string> Audio) ParseAvFoundation(string stderr)
    {
        var video = new List<string>();
        var audio = new List<string>();
        List<string>? current = null;

        foreach (var rawLine in SplitLines(stderr))
        {
            if (rawLine.Contains("AVFoundation video devices:", StringComparison.Ordinal))
            {
                current = video;
                continue;
            }

            if (rawLine.Contains("AVFoundation audio devices:", StringComparison.Ordinal))
            {
                current = audio;
                continue;
            }

            if (current is null)
            {
                continue;
            }

            var match = AvFoundationDeviceLine().Match(rawLine);
            if (!match.Success)
            {
                continue;
            }

            var name = match.Groups["name"].Value.Trim();
            if (current == video && name.StartsWith("Capture screen", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            current.Add(name);
        }

        return (video, audio);
    }

    /// <summary>
    /// Parses <c>system_profiler SPCameraDataType -json</c> into a name → unique id map.
    /// </summary>
    /// <param name="json">The JSON output.</param>
    /// <returns>Camera unique ids keyed by name (case-insensitive).</returns>
    public static IReadOnlyDictionary<string, string> ParseSystemProfilerCameras(string json)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(json))
        {
            return result;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("SPCameraDataType", out var cameras)
                || cameras.ValueKind != JsonValueKind.Array)
            {
                return result;
            }

            foreach (var camera in cameras.EnumerateArray())
            {
                if (camera.TryGetProperty("_name", out var name)
                    && camera.TryGetProperty("spcamera_unique-id", out var id)
                    && name.GetString() is { Length: > 0 } n
                    && id.GetString() is { Length: > 0 } i)
                {
                    result[n] = i;
                }
            }
        }
        catch (JsonException)
        {
            // Malformed output; fall back to name-based ids.
        }

        return result;
    }

    /// <summary>
    /// Parses <c>ffmpeg -f dshow -list_devices true -i dummy</c> stderr (both the ffmpeg 5+ inline format
    /// and the older section-header format).
    /// </summary>
    /// <param name="stderr">ffmpeg stderr.</param>
    /// <returns>Video and audio devices with their DirectShow alternative names, when present.</returns>
    public static (IReadOnlyList<DshowDevice> Video, IReadOnlyList<DshowDevice> Audio) ParseDirectShow(string stderr)
    {
        var video = new List<DshowDevice>();
        var audio = new List<DshowDevice>();
        List<DshowDevice>? section = null;
        List<DshowDevice>? lastList = null;

        foreach (var line in SplitLines(stderr))
        {
            if (line.Contains("DirectShow video devices", StringComparison.Ordinal))
            {
                section = video;
                continue;
            }

            if (line.Contains("DirectShow audio devices", StringComparison.Ordinal))
            {
                section = audio;
                continue;
            }

            var alt = DshowAlternativeName().Match(line);
            if (alt.Success)
            {
                if (lastList is { Count: > 0 })
                {
                    var last = lastList[^1];
                    lastList[^1] = last with { AlternativeName = alt.Groups["alt"].Value };
                }

                continue;
            }

            var device = DshowDeviceLine().Match(line);
            if (!device.Success)
            {
                continue;
            }

            var name = device.Groups["name"].Value;
            var kind = device.Groups["kind"].Success ? device.Groups["kind"].Value : null;
            var target = kind switch
            {
                "video" => video,
                "audio" => audio,
                null => section,
                _ => null,
            };

            lastList = target;
            target?.Add(new DshowDevice(name, null));
        }

        return (video, audio);
    }

    /// <summary>
    /// Parses <c>arecord -l</c> output into ALSA capture devices.
    /// </summary>
    /// <param name="output">arecord stdout.</param>
    /// <returns>Audio devices with <c>hw:CARD=x,DEV=n</c> inputs.</returns>
    public static IReadOnlyList<AudioDeviceInfo> ParseArecord(string output)
    {
        var result = new List<AudioDeviceInfo>();
        foreach (var line in SplitLines(output))
        {
            var match = ArecordLine().Match(line);
            if (!match.Success)
            {
                continue;
            }

            var cardId = match.Groups["cardId"].Value;
            var cardName = match.Groups["cardName"].Value;
            var dev = match.Groups["dev"].Value;
            result.Add(new AudioDeviceInfo(cardName, $"hw:CARD={cardId},DEV={dev}"));
        }

        return result;
    }

    private static IEnumerable<string> SplitLines(string text) =>
        (text ?? string.Empty).Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    [GeneratedRegex(@"\]\s*\[(?<index>\d+)\]\s+(?<name>.+)$")]
    private static partial Regex AvFoundationDeviceLine();

    // The line prefix varies by ffmpeg version: "[dshow @ 0x…]" (≤ 7.0) or "[in#0 @ 0x…]" (7.1+).
    [GeneratedRegex("^\\[[^\\]]+ @ [^\\]]+\\]\\s+\"(?<name>[^\"]+)\"(?:\\s+\\((?<kind>video|audio|none)\\))?\\s*$")]
    private static partial Regex DshowDeviceLine();

    [GeneratedRegex("Alternative name\\s+\"(?<alt>[^\"]+)\"")]
    private static partial Regex DshowAlternativeName();

    [GeneratedRegex(@"^card\s+\d+:\s+(?<cardId>[^\s]+)\s+\[(?<cardName>[^\]]+)\],\s+device\s+(?<dev>\d+):")]
    private static partial Regex ArecordLine();
}

/// <summary>
/// A DirectShow device as listed by ffmpeg.
/// </summary>
/// <param name="Name">Friendly name.</param>
/// <param name="AlternativeName">Unique moniker (<c>@device_pnp_...</c>), when reported.</param>
public sealed record DshowDevice(string Name, string? AlternativeName);
