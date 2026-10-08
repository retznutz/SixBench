using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixBench.Common.Dtos;
using SixBench.Common.Enums;
using SixBench.Common.Security;
using SixBench.Services.Roku;

namespace SixBench.Api.Controllers;

/// <summary>
/// Roku developer tools (ECP developer queries). The Roku must have developer mode on; most queries
/// only return data while a sideloaded (dev) channel is in the foreground. A refusal returns 409.
/// </summary>
/// <param name="devTools">Developer tools service.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/roku-devices/{id:int}/dev-tools")]
[Produces("application/json")]
public sealed class RokuDevToolsController(IRokuDevToolsService devTools) : ControllerBase
{
    /// <summary>
    /// Dumps the SceneGraph node tree of the foreground channel.
    /// </summary>
    /// <param name="id">Device id.</param>
    /// <param name="scope">All nodes, un-parented roots, or nodes matching <paramref name="nodeId"/>.</param>
    /// <param name="nodeId">Node id to match (required when scope is Nodes).</param>
    /// <param name="sizes">Include each node's memory use.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The node tree.</returns>
    [HttpGet("sgnodes")]
    [ProducesResponseType<SgNodesDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<SgNodesDto>> SgNodes(
        int id,
        [FromQuery] SgNodeScope scope = SgNodeScope.All,
        [FromQuery] string? nodeId = null,
        [FromQuery] bool sizes = false,
        CancellationToken ct = default) =>
        Ok(await devTools.GetSgNodesAsync(id, scope, nodeId, sizes, ct));

    /// <summary>
    /// Reads current CPU and memory use of the foreground channel.
    /// </summary>
    /// <param name="id">Device id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>One sample.</returns>
    [HttpGet("chanperf")]
    [ProducesResponseType<ChanPerfDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ChanPerfDto>> ChanPerf(int id, CancellationToken ct) =>
        Ok(await devTools.GetChanPerfAsync(id, ct));

    /// <summary>
    /// Reads a channel's persistent registry. Admin only: registries often hold account ids and tokens.
    /// </summary>
    /// <param name="id">Device id.</param>
    /// <param name="appId">Channel id; <c>dev</c> for the sideloaded channel.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The registry.</returns>
    [HttpGet("registry/{appId}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType<RokuRegistryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<RokuRegistryDto>> Registry(int id, string appId, CancellationToken ct) =>
        Ok(await devTools.GetRegistryAsync(id, appId, ct));
}
