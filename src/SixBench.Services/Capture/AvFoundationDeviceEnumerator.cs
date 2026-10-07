using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixBench.Common.Enums;
using SixBench.Common.Options;
using SixBench.Services.Processes;

namespace SixBench.Services.Capture;

/// <summary>
/// macOS enumerator using ffmpeg's AVFoundation device list plus <c>system_profiler</c> for stable ids.
/// </summary>
/// <param name="runner">Process runner.</param>
/// <param name="options">ffmpeg options.</param>
/// <param name="logger">Logger.</param>
public sealed class AvFoundationDeviceEnumerator(
    IProcessRunner runner,
    IOptions<FfmpegOptions> options,
    ILogger<AvFoundationDeviceEnumerator> logger) : ICaptureDeviceEnumerator
{
    /// <inheritdoc />
    public HostPlatform Platform => HostPlatform.MacOS;

    /// <inheritdoc />
    public async Task<DeviceInventory> EnumerateAsync(CancellationToken ct = default)
    {
        var timeout = TimeSpan.FromMilliseconds(options.Value.ProbeTimeoutMs);

        // -list_devices always exits non-zero ("Error opening input"); the list is on stderr.
        var listTask = runner.RunAsync(
            options.Value.Path,
            ["-hide_banner", "-f", "avfoundation", "-list_devices", "true", "-i", ""],
            timeout,
            ct);
        var profilerTask = runner.RunAsync("system_profiler", ["SPCameraDataType", "-json"], timeout, ct);
        await Task.WhenAll(listTask, profilerTask);

        var (videoNames, audioNames) = DeviceListParsers.ParseAvFoundation((await listTask).StandardError);
        var uniqueIds = DeviceListParsers.ParseSystemProfilerCameras((await profilerTask).StandardOutput);

        if (videoNames.Count == 0)
        {
            logger.LogWarning("No AVFoundation video devices found. ffmpeg output: {Output}", (await listTask).StandardError);
        }

        var video = videoNames
            .Select(name => new VideoDeviceInfo(
                uniqueIds.TryGetValue(name, out var id) ? $"avf:{id}" : $"avf-name:{name}",
                name,
                name))
            .ToList();
        var audio = audioNames.Select(name => new AudioDeviceInfo(name, name)).ToList();
        return new DeviceInventory(Platform, video, audio);
    }
}
