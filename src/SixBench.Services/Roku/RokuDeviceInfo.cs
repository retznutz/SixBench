using System.Xml.Linq;

namespace SixBench.Services.Roku;

/// <summary>
/// Fields from the Roku ECP <c>/query/device-info</c> response.
/// </summary>
/// <param name="SerialNumber">Serial number.</param>
/// <param name="FriendlyName">Best available user-facing name.</param>
/// <param name="Model">Model name.</param>
public sealed record RokuDeviceInfo(string SerialNumber, string FriendlyName, string? Model)
{
    /// <summary>
    /// Parses the <c>device-info</c> XML document.
    /// </summary>
    /// <param name="xml">Response body.</param>
    /// <returns>The parsed info.</returns>
    /// <exception cref="FormatException">The XML has no serial number.</exception>
    public static RokuDeviceInfo Parse(string xml)
    {
        var root = XDocument.Parse(xml).Root ?? throw new FormatException("Empty device-info response.");
        string? Get(string name) => root.Element(name)?.Value is { Length: > 0 } v ? v.Trim() : null;

        var serial = Get("serial-number") ?? throw new FormatException("device-info has no serial-number.");
        var model = Get("model-name");
        var name = Get("user-device-name")
            ?? Get("friendly-device-name")
            ?? Get("default-device-name")
            ?? model
            ?? $"Roku {serial}";
        return new RokuDeviceInfo(serial, name, model);
    }
}
