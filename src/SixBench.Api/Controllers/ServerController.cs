using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixBench.Api.Infrastructure;
using SixBench.Common.Security;

namespace SixBench.Api.Controllers;

/// <summary>
/// Server control (administrators only).
/// </summary>
/// <param name="restarter">Restarter.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/server")]
[Produces("application/json")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class ServerController(ServerRestarter restarter) : ControllerBase
{
    /// <summary>
    /// Restarts the server. Unless <c>Server:SelfRestart</c> is false, a new process is started before this one exits.
    /// </summary>
    /// <returns>Whether a new process was started.</returns>
    [HttpPost("restart")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public IActionResult Restart()
    {
        var relaunched = restarter.Restart();
        return Accepted(new { relaunched, message = relaunched ? "Server is restarting..." : "Server is stopping; your service manager should start it again." });
    }
}
