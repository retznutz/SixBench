using Microsoft.Extensions.Options;
using SixBench.Common.Dtos;
using SixBench.Common.Options;
using SixBench.Common.Utilities;
using SixBench.Data.Entities;
using SixBench.Data.Repositories;
using SixBench.Services.Exceptions;
using SixBench.Services.Mapping;
using SixBench.Services.Streaming;

namespace SixBench.Services.Capture;

/// <summary>
/// Default <see cref="ICaptureDeviceService"/>.
/// </summary>
/// <param name="inventory">Device inventory provider.</param>
/// <param name="links">Encoder link repository.</param>
/// <param name="audioPolicy">Audio policy.</param>
/// <param name="ffmpegOptions">ffmpeg options (capture defaults).</param>
/// <param name="thumbnails">Encoder thumbnails.</param>
public sealed class CaptureDeviceService(
    IDeviceInventoryProvider inventory,
    IEncoderLinkRepository links,
    IAudioPolicy audioPolicy,
    IOptions<FfmpegOptions> ffmpegOptions,
    IEncoderThumbnailStore thumbnails) : ICaptureDeviceService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<CaptureDeviceDto>> ListAsync(bool refresh = false, CancellationToken ct = default)
    {
        var detected = await inventory.GetAsync(refresh, ct);
        var savedLinks = (await links.ListAsync(ct)).ToDictionary(l => l.CaptureDeviceStableId, StringComparer.Ordinal);

        var result = new List<CaptureDeviceDto>();
        foreach (var video in detected.Video)
        {
            savedLinks.Remove(video.StableId, out var link);
            result.Add(ToDto(detected, video, link));
        }

        // Saved links for devices that are not plugged in right now.
        result.AddRange(savedLinks.Values.Select(link => new CaptureDeviceDto(
            DeviceKey.Encode(link.CaptureDeviceStableId),
            link.CaptureDeviceStableId,
            link.DisplayName,
            link.VideoInput,
            link.AudioInput,
            detected.Platform,
            IsConnected: false,
            link.ToDto(),
            thumbnails.Find(link.CaptureDeviceStableId)?.UpdatedUtc)));

        return result
            .OrderByDescending(d => d.IsConnected)
            .ThenByDescending(d => d.Link is not null)
            .ThenBy(d => d.Link?.DisplayName ?? d.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<CaptureDeviceDto> GetAsync(string stableId, CancellationToken ct = default)
    {
        var all = await ListAsync(false, ct);
        return all.FirstOrDefault(d => d.StableId == stableId)
            ?? throw new NotFoundException($"Capture device '{stableId}' was not found.");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AudioInputDto>> ListAudioInputsAsync(CancellationToken ct = default) =>
        (await inventory.GetAsync(false, ct)).Audio.Select(a => new AudioInputDto(a.Name, a.Input)).ToList();

    /// <inheritdoc />
    public async Task<CaptureSource> ResolveSourceAsync(string stableId, CancellationToken ct = default)
    {
        var detected = await inventory.GetAsync(false, ct);
        var video = detected.Video.FirstOrDefault(v => v.StableId == stableId);
        if (video is null)
        {
            // The cache may predate a hot-plug; look once more before giving up.
            detected = await inventory.GetAsync(true, ct);
            video = detected.Video.FirstOrDefault(v => v.StableId == stableId)
                ?? throw new NotFoundException($"Capture device '{stableId}' is not connected.");
        }

        var link = await links.GetByStableIdAsync(stableId, ct);
        var audioInput = ResolveAudioInput(detected, video, link);
        var defaults = ffmpegOptions.Value;

        return new CaptureSource(
            stableId,
            link?.DisplayName ?? video.Name,
            detected.Platform,
            string.IsNullOrWhiteSpace(link?.VideoInput) ? video.Input : link.VideoInput,
            audioInput,
            link?.FrameRate ?? defaults.DefaultFrameRate,
            link?.VideoSize ?? defaults.DefaultVideoSize,
            link?.PixelFormat ?? defaults.DefaultPixelFormat,
            audioPolicy.Evaluate(link, audioInput));
    }

    private CaptureDeviceDto ToDto(DeviceInventory detected, VideoDeviceInfo video, EncoderLink? link) => new(
        DeviceKey.Encode(video.StableId),
        video.StableId,
        video.Name,
        video.Input,
        ResolveAudioInput(detected, video, link),
        detected.Platform,
        IsConnected: true,
        link?.ToDto(),
        thumbnails.Find(video.StableId)?.UpdatedUtc);

    private static string? ResolveAudioInput(DeviceInventory detected, VideoDeviceInfo video, EncoderLink? link) =>
        !string.IsNullOrWhiteSpace(link?.AudioInput)
            ? link.AudioInput
            : AudioPairingHeuristic.Pair(video.Name, detected.Audio)?.Input;
}
