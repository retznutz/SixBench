using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using SixBench.Api.Infrastructure;
using SixBench.Common.Dtos;
using SixBench.Services.Streaming;

namespace SixBench.Api.Controllers;

/// <summary>
/// Live capture streams: the WebSocket endpoint and diagnostics.
/// </summary>
/// <param name="sessions">Session manager.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/streams")]
[Produces("application/json")]
public sealed class StreamsController(ICaptureSessionManager sessions) : ControllerBase
{
    /// <summary>
    /// Lists active capture sessions with per-client stats.
    /// </summary>
    /// <returns>Active sessions.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<StreamSessionDto>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<StreamSessionDto>> List() => Ok(sessions.GetSessions());

    /// <summary>
    /// WebSocket endpoint that streams an encoder. Not callable from Swagger; see the README for the protocol.
    /// </summary>
    /// <param name="id">Capture device id.</param>
    /// <param name="handler">Socket handler.</param>
    /// <returns>Nothing once the socket closes; 400 when the request is not a WebSocket upgrade.</returns>
    [HttpGet("{id}/ws")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> Connect(string id, [FromServices] IStreamSocketHandler handler)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "WebSocket upgrade required");
        }

        var stableId = RouteKeys.ToStableId(id);
        using var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        await handler.HandleAsync(socket, stableId, HttpContext.RequestAborted);
        return new EmptyResult();
    }
}
