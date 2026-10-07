using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using SixBench.Api.Infrastructure;
using SixBench.Common.Dtos;
using SixBench.Services.Links;

namespace SixBench.Api.Controllers;

/// <summary>
/// Saved encoder settings and the Roku each encoder is connected to.
/// </summary>
/// <param name="links">Link service.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/encoder-links")]
[Produces("application/json")]
public sealed class EncoderLinksController(IEncoderLinkService links) : ControllerBase
{
    /// <summary>
    /// Lists all saved links.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The links.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<EncoderLinkDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EncoderLinkDto>>> List(CancellationToken ct) =>
        Ok(await links.ListAsync(ct));

    /// <summary>
    /// Gets the link for an encoder.
    /// </summary>
    /// <param name="id">Capture device id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The link.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType<EncoderLinkDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EncoderLinkDto>> Get(string id, CancellationToken ct) =>
        Ok(await links.GetAsync(RouteKeys.ToStableId(id), ct));

    /// <summary>
    /// Creates or updates the link for an encoder. Active streams restart to apply new capture settings.
    /// </summary>
    /// <param name="id">Capture device id.</param>
    /// <param name="request">Link settings.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The saved link.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType<EncoderLinkDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EncoderLinkDto>> Upsert(string id, UpsertEncoderLinkRequest request, CancellationToken ct) =>
        Ok(await links.UpsertAsync(RouteKeys.ToStableId(id), request, ct));

    /// <summary>
    /// Deletes the link for an encoder.
    /// </summary>
    /// <param name="id">Capture device id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 when deleted, 404 when no link existed.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct) =>
        await links.DeleteAsync(RouteKeys.ToStableId(id), ct) ? NoContent() : NotFound();
}
