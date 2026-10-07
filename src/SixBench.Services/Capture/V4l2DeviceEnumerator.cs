using Microsoft.Extensions.Logging;
using SixBench.Common.Enums;
using SixBench.Common.Utilities;
using SixBench.Services.Processes;

namespace SixBench.Services.Capture;

/// <summary>
/// Linux enumerator. Video devices come from <c>/dev/v4l/by-id</c> (stable across reboots and re-plugging,
/// unlike <c>/dev/videoN</c>); audio devices come from <c>arecord -l</c>.
/// </summary>
/// <param name="runner">Process runner.</param>
/// <param name="logger">Logger.</param>
public sealed class V4l2DeviceEnumerator(IProcessRunner runner, ILogger<V4l2DeviceEnumerator> logger)
    : ICaptureDeviceEnumerator
{
    private const string ByIdDirectory = "/dev/v4l/by-id";

    /// <inheritdoc />
    public HostPlatform Platform => HostPlatform.Linux;

    /// <inheritdoc />
    public async Task<DeviceInventory> EnumerateAsync(CancellationToken ct = default)
    {
        var video = new List<VideoDeviceInfo>();

        if (Directory.Exists(ByIdDirectory))
        {
            foreach (var link in Directory.EnumerateFileSystemEntries(ByIdDirectory).Order())
            {
                // Each UVC device exposes a capture node (index0) and a metadata node (index1).
                if (!link.EndsWith("-video-index0", StringComparison.Ordinal))
                {
                    continue;
                }

                var target = new FileInfo(link).LinkTarget;
                var node = target is null ? null : Path.GetFileName(target);
                var fileName = Path.GetFileName(link);
                video.Add(new VideoDeviceInfo($"v4l2:{fileName}", ReadSysfsName(node) ?? fileName, PathUtil.Normalize(link)));
            }
        }
        else
        {
            foreach (var dev in Directory.EnumerateFiles("/dev", "video*").Order())
            {
                var node = Path.GetFileName(dev);
                video.Add(new VideoDeviceInfo($"v4l2-path:{dev}", ReadSysfsName(node) ?? node, dev));
            }
        }

        var arecord = await runner.RunAsync("arecord", ["-l"], TimeSpan.FromSeconds(5), ct);
        if (arecord.ExitCode != 0)
        {
            logger.LogWarning("arecord -l failed ({ExitCode}); audio devices unavailable. Install alsa-utils.", arecord.ExitCode);
        }

        return new DeviceInventory(Platform, video, DeviceListParsers.ParseArecord(arecord.StandardOutput));
    }

    private static string? ReadSysfsName(string? node)
    {
        if (string.IsNullOrEmpty(node))
        {
            return null;
        }

        var path = $"/sys/class/video4linux/{node}/name";
        return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
    }
}
