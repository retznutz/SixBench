namespace SixBench.Common.Options;

/// <summary>
/// Roku discovery and control settings, bound from the <c>Roku</c> configuration section.
/// </summary>
public sealed class RokuOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Roku";

    /// <summary>How long to collect SSDP responses, in milliseconds.</summary>
    public int DiscoveryTimeoutMs { get; set; } = 3000;

    /// <summary>Default ECP port.</summary>
    public int EcpPort { get; set; } = 8060;

    /// <summary>HTTP timeout for ECP requests, in milliseconds.</summary>
    public int RequestTimeoutMs { get; set; } = 3000;

    /// <summary>Delay between characters when typing text, in milliseconds.</summary>
    public int TextCharDelayMs { get; set; } = 25;
}
