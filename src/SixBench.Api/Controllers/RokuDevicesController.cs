using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using SixBench.Common.Dtos;
using SixBench.Services.Roku;

namespace SixBench.Api.Controllers;

/// <summary>
/// Roku devices: discovery, manual registration and remote control.
/// </summary>
/// <param name="devices">Roku device service.</param>
/// <param name="control">Roku control service.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/roku-devices")]
[Produces("application/json")]
public sealed class RokuDevicesController(IRokuDeviceService devices, IRokuControlService control) : ControllerBase
{
    /// <summary>
    /// Lists saved Roku devices.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The devices.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RokuDeviceDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RokuDeviceDto>>> List(CancellationToken ct) =>
        Ok(await devices.ListAsync(ct));

    /// <summary>
    /// Gets a saved Roku device.
    /// </summary>
    /// <param name="id">Device id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The device.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType<RokuDeviceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RokuDeviceDto>> Get(int id, CancellationToken ct) =>
        Ok(await devices.GetAsync(id, ct));

    /// <summary>
    /// Searches the LAN for Roku devices (SSDP, a few seconds) and saves what it finds.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The devices found.</returns>
    [HttpPost("discover")]
    [ProducesResponseType<IReadOnlyList<RokuDeviceDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RokuDeviceDto>>> Discover(CancellationToken ct) =>
        Ok(await devices.DiscoverAsync(ct));

    /// <summary>
    /// Adds a Roku by IP address after checking that it responds.
    /// </summary>
    /// <param name="request">Host and optional port.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The saved device.</returns>
    [HttpPost]
    [ProducesResponseType<RokuDeviceDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<RokuDeviceDto>> Add(AddRokuRequest request, CancellationToken ct)
    {
        var device = await devices.AddManualAsync(request.Host, request.Port, ct);
        return CreatedAtAction(nameof(Get), new { id = device.Id, version = "1.0" }, device);
    }

    /// <summary>
    /// Deletes a saved Roku; encoders linked to it become unlinked.
    /// </summary>
    /// <param name="id">Device id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 when deleted.</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        await devices.DeleteAsync(id, ct) ? NoContent() : NotFound();

    /// <summary>
    /// Sends a remote-control key.
    /// </summary>
    /// <param name="id">Device id.</param>
    /// <param name="request">Key and action.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 when sent.</returns>
    [HttpPost("{id:int}/keys")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SendKey(int id, KeyCommandRequest request, CancellationToken ct)
    {
        await control.SendKeyAsync(id, request.Key, request.Action, ct);
        return NoContent();
    }

    /// <summary>
    /// Types text into the Roku's on-screen keyboard.
    /// </summary>
    /// <param name="id">Device id.</param>
    /// <param name="request">The text.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 when sent.</returns>
    [HttpPost("{id:int}/text")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SendText(int id, TextInputRequest request, CancellationToken ct)
    {
        await control.SendTextAsync(id, request.Text, ct);
        return NoContent();
    }
}
