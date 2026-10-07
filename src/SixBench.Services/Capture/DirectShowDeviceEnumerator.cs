using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixBench.Common.Enums;
using SixBench.Common.Options;
using SixBench.Services.Processes;

namespace SixBench.Services.Capture;

/// <summary>
/// Windows enumerator using ffmpeg's DirectShow device list. The DirectShow "alternative name"
/// (PnP moniker) is used as both the stable id and the ffmpeg input, since friendly names can collide.
/// </summary>
/// <param name="runner">Process runner.</param>
/// <param name="options">ffmpeg options.</param>
/// <param name="logger">Logger.</param>
public sealed class DirectShowDeviceEnumerator(
    IProcessRunner runner,
    IOptions<FfmpegOptions> options,
    ILogger<DirectShowDeviceEnumerator> logger) : ICaptureDeviceEnumerator
{
    /// <inheritdoc />
    public HostPlatform Platform => HostPlatform.Windows;

    /// <inheritdoc />
    public async Task<DeviceInventory> EnumerateAsync(CancellationToken ct = default)
    {
        var result = await runner.RunAsync(
            options.Value.Path,
            ["-hide_banner", "-f", "dshow", "-list_devices", "true", "-i", "dummy"],
            TimeSpan.FromMilliseconds(options.Value.ProbeTimeoutMs),
            ct);

        var (video, audio) = DeviceListParsers.ParseDirectShow(result.StandardError);
        if (video.Count == 0)
        {
            logger.LogWarning("No DirectShow video devices found. ffmpeg output: {Output}", result.StandardError);
        }

        return new DeviceInventory(
            Platform,
            video.Select(v => new VideoDeviceInfo(
                v.AlternativeName is null ? $"dshow-name:{v.Name}" : $"dshow:{v.AlternativeName}",
                v.Name,
                v.AlternativeName ?? v.Name)).ToList(),
            audio.Select(a => new AudioDeviceInfo(a.Name, a.AlternativeName ?? a.Name)).ToList());
    }
}
