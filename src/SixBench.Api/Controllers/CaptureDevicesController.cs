using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using SixBench.Api.Infrastructure;
using SixBench.Common.Dtos;
using SixBench.Services.Capture;

namespace SixBench.Api.Controllers;

/// <summary>
/// HDMI capture encoders attached to the host.
/// </summary>
/// <param name="devices">Capture device service.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/capture-devices")]
[Produces("application/json")]
public sealed class CaptureDevicesController(ICaptureDeviceService devices) : ControllerBase
{
    /// <summary>
    /// Lists detected encoders, merged with saved links. Saved encoders that are unplugged are included with <c>isConnected=false</c>.
    /// </summary>
    /// <param name="refresh">Re-enumerate devices instead of using the 5-second cache.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The encoders.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CaptureDeviceDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CaptureDeviceDto>>> List([FromQuery] bool refresh, CancellationToken ct) =>
        Ok(await devices.ListAsync(refresh, ct));

    /// <summary>
    /// Lists the host's audio capture devices, for choosing an encoder's audio input.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Audio inputs.</returns>
    [HttpGet("audio-inputs")]
    [ProducesResponseType<IReadOnlyList<AudioInputDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AudioInputDto>>> ListAudioInputs(CancellationToken ct) =>
        Ok(await devices.ListAudioInputsAsync(ct));

    /// <summary>
    /// Gets one encoder.
    /// </summary>
    /// <param name="id">Device id (from the list response).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The encoder.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType<CaptureDeviceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CaptureDeviceDto>> Get(string id, CancellationToken ct) =>
        Ok(await devices.GetAsync(RouteKeys.ToStableId(id), ct));
}
