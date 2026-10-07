using SixBench.Common.Dtos;
using SixBench.Data.Entities;
using SixBench.Data.Repositories;
using SixBench.Services.Capture;
using SixBench.Services.Exceptions;
using SixBench.Services.Mapping;
using SixBench.Services.Streaming;

namespace SixBench.Services.Links;

/// <summary>
/// Default <see cref="IEncoderLinkService"/>.
/// </summary>
/// <param name="links">Link repository.</param>
/// <param name="rokus">Roku repository.</param>
/// <param name="inventory">Device inventory (to default the video input).</param>
/// <param name="sessions">Session manager (to restart streams after changes).</param>
public sealed class EncoderLinkService(
    IEncoderLinkRepository links,
    IRokuDeviceRepository rokus,
    IDeviceInventoryProvider inventory,
    ICaptureSessionManager sessions) : IEncoderLinkService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<EncoderLinkDto>> ListAsync(CancellationToken ct = default) =>
        (await links.ListAsync(ct)).Select(l => l.ToDto()).ToList();

    /// <inheritdoc />
    public async Task<EncoderLinkDto> GetAsync(string stableId, CancellationToken ct = default)
    {
        var link = await links.GetByStableIdAsync(stableId, ct)
            ?? throw new NotFoundException($"No link exists for capture device '{stableId}'.");
        return link.ToDto();
    }

    /// <inheritdoc />
    public async Task<EncoderLinkDto> UpsertAsync(
        string stableId,
        UpsertEncoderLinkRequest request,
        CancellationToken ct = default)
    {
        if (request.RokuDeviceId is { } rokuId && await rokus.GetAsync(rokuId, ct) is null)
        {
            throw new ServiceValidationException($"Roku device {rokuId} does not exist.");
        }

        var link = await links.GetByStableIdAsync(stableId, ct) ?? new EncoderLink { CaptureDeviceStableId = stableId };

        var videoInput = request.VideoInput;
        if (string.IsNullOrWhiteSpace(videoInput))
        {
            videoInput = link.VideoInput;
        }

        if (string.IsNullOrWhiteSpace(videoInput))
        {
            var detected = await inventory.GetAsync(false, ct);
            videoInput = detected.Video.FirstOrDefault(v => v.StableId == stableId)?.Input
                ?? throw new NotFoundException($"Capture device '{stableId}' is not connected; specify videoInput.");
        }

        link.DisplayName = request.DisplayName.Trim();
        link.VideoInput = videoInput;
        link.AudioInput = string.IsNullOrWhiteSpace(request.AudioInput) ? null : request.AudioInput.Trim();
        link.RokuDeviceId = request.RokuDeviceId;
        link.AllowDeviceAudio = request.AllowDeviceAudio;
        link.FrameRate = request.FrameRate;
        link.VideoSize = string.IsNullOrWhiteSpace(request.VideoSize) ? null : request.VideoSize.Trim();
        link.PixelFormat = string.IsNullOrWhiteSpace(request.PixelFormat) ? null : request.PixelFormat.Trim();

        var saved = await links.SaveAsync(link, ct);
        await sessions.RestartAsync(stableId, "settings_changed");
        return saved.ToDto();
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string stableId, CancellationToken ct = default)
    {
        var deleted = await links.DeleteAsync(stableId, ct);
        if (deleted)
        {
            await sessions.RestartAsync(stableId, "settings_changed");
        }

        return deleted;
    }
}
