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

    /// <summary>HTTP timeout for developer-tool queries (SceneGraph dumps can be large), in milliseconds.</summary>
    public int DevToolsTimeoutMs { get; set; } = 15000;

    /// <summary>Delay between characters when typing text, in milliseconds.</summary>
    public int TextCharDelayMs { get; set; } = 25;

    /// <summary>Port of the developer-mode web server (installer, packager, utilities).</summary>
    public int DevServerPort { get; set; } = 80;

    /// <summary>User name for the developer-mode web server (HTTP Digest auth).</summary>
    public string DevServerUserName { get; set; } = "rokudev";

    /// <summary>HTTP timeout for developer-mode web server requests (installing and packaging can take a minute), in milliseconds.</summary>
    public int DevServerTimeoutMs { get; set; } = 180000;

    /// <summary>Largest channel zip or package accepted for upload, in megabytes.</summary>
    public int SideloadMaxMegabytes { get; set; } = 512;

    /// <summary>TCP port of the BrightScript debug console.</summary>
    public int DebugConsolePort { get; set; } = 8085;

    /// <summary>How much recent debug console output to keep for viewers who join late, in characters.</summary>
    public int DebugConsoleBacklogChars { get; set; } = 200000;
}
