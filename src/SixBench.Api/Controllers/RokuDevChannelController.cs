using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SixBench.Common.Dtos;
using SixBench.Common.Options;
using SixBench.Common.Security;
using SixBench.Services.Exceptions;
using SixBench.Services.Roku;

namespace SixBench.Api.Controllers;

/// <summary>
/// The Roku developer web page, done by the server: the developer password, sideloading, packaging and utilities.
/// Developer mode must be on. Changes to the Roku are admin only; a refusal by the Roku returns 409.
/// </summary>
/// <param name="devChannel">Developer channel service.</param>
/// <param name="options">Roku options.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/roku-devices/{id:int}")]
[Produces("application/json")]
public sealed class RokuDevChannelController(IRokuDevChannelService devChannel, IOptions<RokuOptions> options) : ControllerBase
{
    // Upper bound for the request pipeline; the configured Roku:SideloadMaxMegabytes is checked per upload.
    private const long MaxUploadBytes = 1025L * 1024 * 1024;

    /// <summary>
    /// Reads developer mode, signing key and sideloaded channel state.
    /// </summary>
    /// <param name="id">Device id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The state.</returns>
    [HttpGet("dev-channel")]
    [ProducesResponseType<RokuDevChannelStatusDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<RokuDevChannelStatusDto>> Status(int id, CancellationToken ct) =>
        Ok(await devChannel.GetStatusAsync(id, ct));

    /// <summary>
    /// Saves the developer-mode password after checking it with the Roku. It is stored encrypted and never returned.
    /// </summary>
    /// <param name="id">Device id.</param>
    /// <param name="request">The password.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated device.</returns>
    [HttpPut("dev-password")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType<RokuDeviceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<RokuDeviceDto>> SetDevPassword(int id, SetDevPasswordRequest request, CancellationToken ct) =>
        Ok(await devChannel.SetDevPasswordAsync(id, request.Password, ct));

    /// <summary>
    /// Forgets the saved developer-mode password.
    /// </summary>
    /// <param name="id">Device id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated device.</returns>
    [HttpDelete("dev-password")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType<RokuDeviceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RokuDeviceDto>> ClearDevPassword(int id, CancellationToken ct) =>
        Ok(await devChannel.ClearDevPasswordAsync(id, ct));

    /// <summary>
    /// Installs (or replaces) the sideloaded channel from a zip. The Roku launches it when done.
    /// </summary>
    /// <param name="id">Device id.</param>
    /// <param name="archive">Channel zip, with the manifest at its root.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The Roku's messages.</returns>
    [HttpPost("dev-channel")]
    [Authorize(Roles = AppRoles.Admin)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
    [ProducesResponseType<RokuDevActionResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<RokuDevActionResultDto>> Install(int id, IFormFile archive, CancellationToken ct) =>
        Ok(await devChannel.InstallAsync(id, await ReadUploadAsync(archive, ct), archive.FileName, ct));

    /// <summary>
    /// Deletes the sideloaded channel.
    /// </summary>
    /// <param name="id">Device id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The Roku's messages.</returns>
    [HttpDelete("dev-channel")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType<RokuDevActionResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<RokuDevActionResultDto>> Delete(int id, CancellationToken ct) =>
        Ok(await devChannel.DeleteAsync(id, ct));

    /// <summary>
    /// Launches the sideloaded channel.
    /// </summary>
    /// <param name="id">Device id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 when sent.</returns>
    [HttpPost("dev-channel/launch")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Launch(int id, CancellationToken ct)
    {
        await devChannel.LaunchAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// Converts the sideloaded channel to squashfs.
    /// </summary>
    /// <param name="id">Device id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The Roku's messages.</returns>
    [HttpPost("dev-channel/squashfs")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType<RokuDevActionResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<RokuDevActionResultDto>> ConvertToSquashfs(int id, CancellationToken ct) =>
        Ok(await devChannel.ConvertToSquashfsAsync(id, ct));

    /// <summary>
    /// Takes a screenshot of the running sideloaded channel (rendered by the Roku, not captured from HDMI).
    /// </summary>
    /// <param name="id">Device id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A JPEG or PNG image.</returns>
    [HttpPost("dev-channel/screenshot")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "image/jpeg", "image/png")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Screenshot(int id, CancellationToken ct)
    {
        var file = await devChannel.ScreenshotAsync(id, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>
    /// Packages the sideloaded channel into a <c>.pkg</c> signed with the Roku's developer key.
    /// The signing password is used for this request only and never stored.
    /// </summary>
    /// <param name="id">Device id.</param>
    /// <param name="request">Name, version and signing password.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The signed package.</returns>
    [HttpPost("dev-channel/package")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "application/octet-stream")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Package(int id, PackageChannelRequest request, CancellationToken ct)
    {
        var file = await devChannel.PackageAsync(id, request, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>
    /// Re-keys the Roku with the developer key inside a previously signed package, so it can sign updates to that channel.
    /// </summary>
    /// <param name="id">Device id.</param>
    /// <param name="package">A <c>.pkg</c> signed with the key to install.</param>
    /// <param name="signingPassword">That key's signing password (never stored).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The Roku's messages.</returns>
    [HttpPost("dev-channel/rekey")]
    [Authorize(Roles = AppRoles.Admin)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
    [ProducesResponseType<RokuDevActionResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<RokuDevActionResultDto>> Rekey(
        int id, IFormFile package, [FromForm] string signingPassword, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(signingPassword))
        {
            throw new ServiceValidationException("The signing password is required.");
        }

        return Ok(await devChannel.RekeyAsync(id, await ReadUploadAsync(package, ct), package.FileName, signingPassword, ct));
    }

    /// <summary>
    /// Reboots the Roku.
    /// </summary>
    /// <param name="id">Device id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The Roku's messages.</returns>
    [HttpPost("dev-channel/reboot")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType<RokuDevActionResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<RokuDevActionResultDto>> Reboot(int id, CancellationToken ct) =>
        Ok(await devChannel.RebootAsync(id, ct));

    /// <summary>
    /// Asks the Roku to check for a software update (some Roku OS versions refuse installs until it has).
    /// </summary>
    /// <param name="id">Device id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The Roku's messages.</returns>
    [HttpPost("dev-channel/check-update")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType<RokuDevActionResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<RokuDevActionResultDto>> CheckForUpdate(int id, CancellationToken ct) =>
        Ok(await devChannel.CheckForUpdateAsync(id, ct));

    private async Task<byte[]> ReadUploadAsync(IFormFile file, CancellationToken ct)
    {
        var maxBytes = (long)options.Value.SideloadMaxMegabytes * 1024 * 1024;
        if (file.Length == 0)
        {
            throw new ServiceValidationException("The uploaded file is empty.");
        }

        if (file.Length > maxBytes)
        {
            throw new ServiceValidationException(
                $"The file is {file.Length / (1024 * 1024)} MB; the limit is {options.Value.SideloadMaxMegabytes} MB (Roku:SideloadMaxMegabytes).");
        }

        var bytes = GC.AllocateUninitializedArray<byte>((int)file.Length);
        await using var stream = file.OpenReadStream();
        await stream.ReadExactlyAsync(bytes, ct);
        return bytes;
    }
}
