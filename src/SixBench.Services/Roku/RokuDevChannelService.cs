using Microsoft.Extensions.Options;
using SixBench.Common.Dtos;
using SixBench.Common.Options;
using SixBench.Data.Entities;
using SixBench.Data.Repositories;
using SixBench.Services.Exceptions;
using SixBench.Services.Mapping;

namespace SixBench.Services.Roku;

/// <summary>
/// The Roku developer web page, done by the server: sideloading, packaging and the utilities.
/// The server talks to the Roku, so this also works for viewers who are not on the Roku's network.
/// </summary>
public interface IRokuDevChannelService
{
    /// <summary>Reads developer mode, signing key and sideloaded channel state (ECP; no password needed).</summary>
    /// <param name="rokuId">Saved device id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The state.</returns>
    /// <exception cref="NotFoundException">No such device.</exception>
    /// <exception cref="RokuUnreachableException">The device did not respond.</exception>
    Task<RokuDevChannelStatusDto> GetStatusAsync(int rokuId, CancellationToken ct = default);

    /// <summary>Checks the developer password against the Roku, then saves it encrypted.</summary>
    /// <param name="rokuId">Saved device id.</param>
    /// <param name="password">The password.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated device.</returns>
    /// <exception cref="NotFoundException">No such device.</exception>
    /// <exception cref="FieldValidationException">The Roku rejected the password.</exception>
    /// <exception cref="RokuUnreachableException">The developer web server did not respond.</exception>
    Task<RokuDeviceDto> SetDevPasswordAsync(int rokuId, string password, CancellationToken ct = default);

    /// <summary>Forgets the saved developer password.</summary>
    /// <param name="rokuId">Saved device id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated device.</returns>
    /// <exception cref="NotFoundException">No such device.</exception>
    Task<RokuDeviceDto> ClearDevPasswordAsync(int rokuId, CancellationToken ct = default);

    /// <summary>Installs (or replaces) the sideloaded channel.</summary>
    /// <param name="rokuId">Saved device id.</param>
    /// <param name="archive">Channel zip.</param>
    /// <param name="fileName">Zip file name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The Roku's messages.</returns>
    /// <exception cref="NotFoundException">No such device.</exception>
    /// <exception cref="ServiceValidationException">No password saved, or the file is not a zip.</exception>
    /// <exception cref="RokuUnreachableException">The device did not respond.</exception>
    /// <exception cref="RokuRequestRejectedException">The Roku refused.</exception>
    Task<RokuDevActionResultDto> InstallAsync(int rokuId, byte[] archive, string fileName, CancellationToken ct = default);

    /// <summary>Deletes the sideloaded channel.</summary>
    /// <param name="rokuId">Saved device id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The Roku's messages.</returns>
    Task<RokuDevActionResultDto> DeleteAsync(int rokuId, CancellationToken ct = default);

    /// <summary>Launches the sideloaded channel (ECP; no password needed).</summary>
    /// <param name="rokuId">Saved device id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task LaunchAsync(int rokuId, CancellationToken ct = default);

    /// <summary>Converts the sideloaded channel to squashfs.</summary>
    /// <param name="rokuId">Saved device id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The Roku's messages.</returns>
    Task<RokuDevActionResultDto> ConvertToSquashfsAsync(int rokuId, CancellationToken ct = default);

    /// <summary>Screenshots the running sideloaded channel.</summary>
    /// <param name="rokuId">Saved device id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The image.</returns>
    Task<RokuDevFile> ScreenshotAsync(int rokuId, CancellationToken ct = default);

    /// <summary>Packages the sideloaded channel into a signed <c>.pkg</c>.</summary>
    /// <param name="rokuId">Saved device id.</param>
    /// <param name="request">Name, version and signing password.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The package.</returns>
    Task<RokuDevFile> PackageAsync(int rokuId, PackageChannelRequest request, CancellationToken ct = default);

    /// <summary>Re-keys the Roku from a signed package.</summary>
    /// <param name="rokuId">Saved device id.</param>
    /// <param name="package">The <c>.pkg</c>.</param>
    /// <param name="fileName">Package file name.</param>
    /// <param name="signingPassword">The package's signing password.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The Roku's messages.</returns>
    Task<RokuDevActionResultDto> RekeyAsync(int rokuId, byte[] package, string fileName, string signingPassword, CancellationToken ct = default);

    /// <summary>Reboots the Roku.</summary>
    /// <param name="rokuId">Saved device id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The Roku's messages.</returns>
    Task<RokuDevActionResultDto> RebootAsync(int rokuId, CancellationToken ct = default);

    /// <summary>Asks the Roku to check for a software update.</summary>
    /// <param name="rokuId">Saved device id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The Roku's messages.</returns>
    Task<RokuDevActionResultDto> CheckForUpdateAsync(int rokuId, CancellationToken ct = default);
}

/// <summary>
/// Default <see cref="IRokuDevChannelService"/>.
/// </summary>
/// <param name="repository">Roku repository.</param>
/// <param name="ecp">ECP client.</param>
/// <param name="devServer">Developer web server client.</param>
/// <param name="protector">Password encryption.</param>
/// <param name="options">Roku options.</param>
public sealed class RokuDevChannelService(
    IRokuDeviceRepository repository,
    IRokuEcpClient ecp,
    IRokuDevServerClient devServer,
    IRokuDevPasswordProtector protector,
    IOptions<RokuOptions> options) : IRokuDevChannelService
{
    /// <summary>App id of the sideloaded channel.</summary>
    public const string DevAppId = "dev";

    /// <inheritdoc />
    public async Task<RokuDevChannelStatusDto> GetStatusAsync(int rokuId, CancellationToken ct = default)
    {
        var device = await LoadAsync(rokuId, ct);
        var infoTask = ecp.GetDeviceInfoAsync(device.IpAddress, device.Port, ct);
        var appsTask = ecp.GetAppsAsync(device.IpAddress, device.Port, ct);
        var info = await infoTask;
        var apps = await appsTask;
        return new RokuDevChannelStatusDto(
            info.DeveloperEnabled,
            info.KeyedDeveloperId,
            device.DevPasswordProtected is not null,
            apps.FirstOrDefault(a => a.Id == DevAppId));
    }

    /// <inheritdoc />
    public async Task<RokuDeviceDto> SetDevPasswordAsync(int rokuId, string password, CancellationToken ct = default)
    {
        var device = await LoadAsync(rokuId, ct);
        try
        {
            await devServer.VerifyPasswordAsync(Target(device, password), ct);
        }
        catch (RokuDevPasswordRejectedException)
        {
            throw new FieldValidationException(new Dictionary<string, string[]>
            {
                ["password"] = [$"{device.FriendlyName} rejected this password. Use the one chosen when developer mode was enabled."],
            });
        }

        await repository.SetDevPasswordAsync(rokuId, protector.Protect(password), ct);
        return (await LoadAsync(rokuId, ct)).ToDto();
    }

    /// <inheritdoc />
    public async Task<RokuDeviceDto> ClearDevPasswordAsync(int rokuId, CancellationToken ct = default)
    {
        if (!await repository.SetDevPasswordAsync(rokuId, null, ct))
        {
            throw new NotFoundException($"Roku device {rokuId} was not found.");
        }

        return (await LoadAsync(rokuId, ct)).ToDto();
    }

    /// <inheritdoc />
    public async Task<RokuDevActionResultDto> InstallAsync(int rokuId, byte[] archive, string fileName, CancellationToken ct = default)
    {
        // Every zip starts with a local file header, "PK\x03\x04".
        if (archive.Length < 4 || archive[0] != 'P' || archive[1] != 'K' || archive[2] != 3 || archive[3] != 4)
        {
            throw new ServiceValidationException("That file is not a zip. Upload the channel as a .zip with the manifest at its root.");
        }

        return await devServer.InstallAsync(await TargetAsync(rokuId, ct), archive, fileName, ct);
    }

    /// <inheritdoc />
    public async Task<RokuDevActionResultDto> DeleteAsync(int rokuId, CancellationToken ct = default) =>
        await devServer.DeleteAsync(await TargetAsync(rokuId, ct), ct);

    /// <inheritdoc />
    public async Task LaunchAsync(int rokuId, CancellationToken ct = default)
    {
        var device = await LoadAsync(rokuId, ct);
        await ecp.LaunchAsync(device.IpAddress, device.Port, DevAppId, ct);
    }

    /// <inheritdoc />
    public async Task<RokuDevActionResultDto> ConvertToSquashfsAsync(int rokuId, CancellationToken ct = default) =>
        await devServer.ConvertToSquashfsAsync(await TargetAsync(rokuId, ct), ct);

    /// <inheritdoc />
    public async Task<RokuDevFile> ScreenshotAsync(int rokuId, CancellationToken ct = default) =>
        await devServer.ScreenshotAsync(await TargetAsync(rokuId, ct), ct);

    /// <inheritdoc />
    public async Task<RokuDevFile> PackageAsync(int rokuId, PackageChannelRequest request, CancellationToken ct = default) =>
        await devServer.PackageAsync(await TargetAsync(rokuId, ct), request.AppName.Trim(), request.Version.Trim(), request.SigningPassword, ct);

    /// <inheritdoc />
    public async Task<RokuDevActionResultDto> RekeyAsync(
        int rokuId, byte[] package, string fileName, string signingPassword, CancellationToken ct = default)
    {
        if (package.Length == 0)
        {
            throw new ServiceValidationException("The package file is empty.");
        }

        return await devServer.RekeyAsync(await TargetAsync(rokuId, ct), package, fileName, signingPassword, ct);
    }

    /// <inheritdoc />
    public async Task<RokuDevActionResultDto> RebootAsync(int rokuId, CancellationToken ct = default) =>
        await devServer.RebootAsync(await TargetAsync(rokuId, ct), ct);

    /// <inheritdoc />
    public async Task<RokuDevActionResultDto> CheckForUpdateAsync(int rokuId, CancellationToken ct = default) =>
        await devServer.CheckForUpdateAsync(await TargetAsync(rokuId, ct), ct);

    private async Task<RokuDevServerTarget> TargetAsync(int rokuId, CancellationToken ct)
    {
        var device = await LoadAsync(rokuId, ct);
        if (device.DevPasswordProtected is null)
        {
            throw new ServiceValidationException(
                $"Save the developer password for {device.FriendlyName} first (Settings > Rokus).");
        }

        var password = protector.Unprotect(device.DevPasswordProtected)
            ?? throw new ServiceValidationException(
                $"The saved developer password for {device.FriendlyName} can no longer be decrypted. Enter it again in Settings > Rokus.");
        return Target(device, password);
    }

    private RokuDevServerTarget Target(RokuDevice device, string password) =>
        new(device.IpAddress, options.Value.DevServerPort, options.Value.DevServerUserName, password);

    private async Task<RokuDevice> LoadAsync(int rokuId, CancellationToken ct) =>
        await repository.GetAsync(rokuId, ct) ?? throw new NotFoundException($"Roku device {rokuId} was not found.");
}
