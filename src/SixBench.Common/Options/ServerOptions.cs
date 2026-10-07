namespace SixBench.Common.Options;

/// <summary>
/// Where the server listens, bound from the <c>Server</c> configuration section.
/// </summary>
public sealed class ServerOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Server";

    /// <summary>Port on all interfaces. Serves HTTPS when TLS is enabled and a certificate exists, otherwise HTTP.</summary>
    public int Port { get; set; } = 5216;

    /// <summary>
    /// When true, a restart requested from the web app starts a new SixBench process before this one exits.
    /// Set to false when a service manager (Windows service, systemd, launchd) restarts SixBench itself.
    /// </summary>
    public bool SelfRestart { get; set; } = true;
}
