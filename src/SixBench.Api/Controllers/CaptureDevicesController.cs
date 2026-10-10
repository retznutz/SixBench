using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using SixBench.Api.Infrastructure;
using SixBench.Common.Dtos;
using SixBench.Services.Capture;
using SixBench.Services.Exceptions;

namespace SixBench.Api.Controllers;

/// <summary>
/// HDMI capture encoders attached to the host.
/// </summary>
/// <param name="devices">Capture device service.</param>
/// <param name="thumbnails">Encoder thumbnails.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/capture-devices")]
[Produces("application/json")]
public sealed class CaptureDevicesController(ICaptureDeviceService devices, IEncoderThumbnailStore thumbnails) : ControllerBase
{
    // Upper bound for the request pipeline; the configured Thumbnails:MaxKilobytes is checked per upload.
    private const long MaxThumbnailUploadBytes = 11L * 1024 * 1024;

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

    /// <summary>
    /// Gets an encoder's thumbnail: a frame the web app saved from its live video.
    /// </summary>
    /// <param name="id">Device id (from the list response).</param>
    /// <returns>A JPEG image.</returns>
    [HttpGet("{id}/thumbnail")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "image/jpeg")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public IActionResult GetThumbnail(string id)
    {
        var thumbnail = thumbnails.Find(RouteKeys.ToStableId(id))
            ?? throw new NotFoundException("This encoder has no thumbnail yet.");
        return PhysicalFile(
            thumbnail.Path,
            "image/jpeg",
            thumbnail.UpdatedUtc,
            new EntityTagHeaderValue($"\"{thumbnail.UpdatedUtc.Ticks:x}\""));
    }

    /// <summary>
    /// Saves an encoder's thumbnail, replacing any existing one. The web app sends a frame from the live video.
    /// </summary>
    /// <param name="id">Device id (from the list response).</param>
    /// <param name="image">The JPEG image.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The encoder, with its new thumbnail time.</returns>
    [HttpPut("{id}/thumbnail")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxThumbnailUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxThumbnailUploadBytes)]
    [ProducesResponseType<CaptureDeviceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CaptureDeviceDto>> SetThumbnail(string id, IFormFile image, CancellationToken ct)
    {
        var stableId = RouteKeys.ToStableId(id);

        // Only known encoders get a file.
        await devices.GetAsync(stableId, ct);

        var bytes = GC.AllocateUninitializedArray<byte>((int)image.Length);
        await using (var stream = image.OpenReadStream())
        {
            await stream.ReadExactlyAsync(bytes, ct);
        }

        await thumbnails.SaveAsync(stableId, bytes, ct);
        return Ok(await devices.GetAsync(stableId, ct));
    }
}
