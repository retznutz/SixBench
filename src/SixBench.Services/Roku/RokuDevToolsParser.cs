using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using SixBench.Common.Dtos;
using SixBench.Common.Enums;
using SixBench.Services.Exceptions;

namespace SixBench.Services.Roku;

/// <summary>
/// Parses the XML returned by the ECP developer queries (<c>sgnodes</c>, <c>chanperf</c>, <c>registry</c>).
/// Each response carries a <c>&lt;status&gt;</c> element; anything other than <c>OK</c> becomes a
/// <see cref="RokuRequestRejectedException"/> with the Roku's error text.
/// </summary>
public static class RokuDevToolsParser
{
    /// <summary>Elements in a response envelope that are metadata rather than data.</summary>
    private static readonly HashSet<string> EnvelopeElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "status", "error", "error-message", "timestamp",
    };

    /// <summary>
    /// Parses a <c>/query/sgnodes/*</c> response.
    /// </summary>
    /// <param name="xml">Response body.</param>
    /// <param name="scope">The query that produced it.</param>
    /// <param name="retrievedUtc">When it was fetched.</param>
    /// <returns>The node tree.</returns>
    /// <exception cref="RokuRequestRejectedException">The Roku reported a failure.</exception>
    /// <exception cref="FormatException">The body is not the expected XML.</exception>
    public static SgNodesDto ParseSgNodes(string xml, SgNodeScope scope, DateTime retrievedUtc)
    {
        var root = LoadAndCheck(xml, "SceneGraph query");

        // /all wraps nodes in a section element (<All_Nodes>); the other queries' shapes are undocumented.
        // Nodes always carry fields as attributes, so an attribute-less element is treated as a wrapper.
        var nodes = root.Elements()
            .Where(e => !EnvelopeElements.Contains(e.Name.LocalName))
            .SelectMany(e => e.HasAttributes ? [e] : e.Elements())
            .Select(ToNode)
            .ToList();
        return new SgNodesDto(scope, nodes, nodes.Sum(Count), retrievedUtc);
    }

    /// <summary>
    /// Parses a <c>/query/chanperf</c> response.
    /// </summary>
    /// <param name="xml">Response body.</param>
    /// <returns>The sample.</returns>
    /// <exception cref="RokuRequestRejectedException">The Roku reported a failure.</exception>
    /// <exception cref="FormatException">The body is not the expected XML.</exception>
    public static ChanPerfDto ParseChanPerf(string xml)
    {
        var root = LoadAndCheck(xml, "Performance query");
        var plugin = root.Element("plugin") ?? throw new FormatException("chanperf response has no <plugin> element.");
        var cpu = plugin.Element("cpu-percent");
        var memory = plugin.Element("memory");

        return new ChanPerfDto(
            AppId: Text(plugin.Element("id")),
            TimestampMs: Long(root.Element("timestamp")),
            CpuDurationSeconds: Double(cpu?.Element("duration-seconds")),
            CpuUserPercent: Double(cpu?.Element("user")),
            CpuSysPercent: Double(cpu?.Element("sys")),
            MemoryUsedBytes: Long(memory?.Element("used")),
            MemoryResidentBytes: Long(memory?.Element("res")),
            MemoryAnonBytes: Long(memory?.Element("anon")),
            MemoryFileBytes: Long(memory?.Element("file")),
            MemorySharedBytes: Long(memory?.Element("shared")),
            MemorySwapBytes: Long(memory?.Element("swap")),
            ProcessId: (int?)Long(plugin.Element("unsecured")?.Element("process-id")));
    }

    /// <summary>
    /// Parses a <c>/query/registry/{appId}</c> response.
    /// </summary>
    /// <param name="xml">Response body.</param>
    /// <param name="appId">The channel id that was queried.</param>
    /// <returns>The registry.</returns>
    /// <exception cref="RokuRequestRejectedException">The Roku reported a failure.</exception>
    /// <exception cref="FormatException">The body is not the expected XML.</exception>
    public static RokuRegistryDto ParseRegistry(string xml, string appId)
    {
        var root = LoadAndCheck(xml, "Registry query");
        var registry = root.Element("registry") ?? throw new FormatException("registry response has no <registry> element.");

        var sections = registry.Element("sections")?.Elements("section")
            .Select(s => new RokuRegistrySectionDto(
                Text(s.Element("name")) ?? string.Empty,
                s.Element("items")?.Elements("item")
                    .Select(i => new RokuRegistryItemDto(Text(i.Element("key")) ?? string.Empty, i.Element("value")?.Value ?? string.Empty))
                    .ToList() ?? []))
            .ToList() ?? [];

        return new RokuRegistryDto(appId, Text(registry.Element("dev-id")), (int?)Long(registry.Element("space-available")), sections);
    }

    private static XElement LoadAndCheck(string xml, string what)
    {
        XElement root;
        try
        {
            root = XDocument.Parse(xml).Root ?? throw new FormatException("Empty response.");
        }
        catch (XmlException ex)
        {
            throw new FormatException($"Response is not XML: {ex.Message}", ex);
        }

        var status = Text(root.Element("status"));
        if (status is not null && !status.Equals("OK", StringComparison.OrdinalIgnoreCase))
        {
            var error = Text(root.Element("error")) ?? Text(root.Element("error-message"));
            throw new RokuRequestRejectedException(
                $"{what} failed on the Roku{(error is null ? "." : $": {error}")} " +
                "Developer tools only work with developer mode on and, for most queries, a sideloaded (dev) channel in the foreground.");
        }

        return root;
    }

    private static SgNodeDto ToNode(XElement element) => new(
        element.Name.LocalName,
        element.Attributes().ToDictionary(a => a.Name.LocalName, a => a.Value),
        element.Elements().Select(ToNode).ToList());

    private static int Count(SgNodeDto node) => 1 + node.Children.Sum(Count);

    private static string? Text(XElement? element) => element?.Value is { Length: > 0 } v ? v.Trim() : null;

    private static long? Long(XElement? element) =>
        long.TryParse(Text(element), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;

    private static double? Double(XElement? element) =>
        double.TryParse(Text(element), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;
}
