using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SixBench.Common.Security;

namespace SixBench.Api.Hubs;

/// <summary>
/// Pushes certificate provisioning progress to administrators
/// (<c>CertificateProvisioningProgress</c>, <c>CertificateProvisioningComplete</c>).
/// </summary>
[Authorize(Roles = AppRoles.Admin)]
public sealed class CertificateHub : Hub
{
    /// <summary>Hub path.</summary>
    public const string Path = "/hubs/certificates";

    /// <summary>Progress event name.</summary>
    public const string ProgressEvent = "CertificateProvisioningProgress";

    /// <summary>Completion event name.</summary>
    public const string CompleteEvent = "CertificateProvisioningComplete";
}
