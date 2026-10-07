using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixBench.Common.Options;
using SixBench.Services.Processes;

namespace SixBench.Services.Ffmpeg;

/// <summary>
/// What the installed ffmpeg can do, as far as SixBench cares.
/// </summary>
/// <param name="Runnable">ffmpeg started and listed its encoders.</param>
/// <param name="VideoEncoder">The configured video encoder.</param>
/// <param name="HasVideoEncoder">The configured video encoder is available.</param>
/// <param name="HasOpus">libopus is available (needed for device audio).</param>
/// <param name="AvailableH264Encoders">Every H.264 encoder this build offers.</param>
/// <param name="Problem">A human-readable description of what is wrong, or null.</param>
public sealed record FfmpegCapabilityReport(
    bool Runnable,
    string VideoEncoder,
    bool HasVideoEncoder,
    bool HasOpus,
    IReadOnlyList<string> AvailableH264Encoders,
    string? Problem);

/// <summary>
/// Checks the installed ffmpeg once and caches the result.
/// </summary>
public interface IFfmpegCapabilities
{
    /// <summary>
    /// Returns the (cached) capability report.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The report.</returns>
    Task<FfmpegCapabilityReport> GetAsync(CancellationToken ct = default);
}

/// <summary>
/// Default <see cref="IFfmpegCapabilities"/>, based on <c>ffmpeg -encoders</c>.
/// </summary>
/// <param name="runner">Process runner.</param>
/// <param name="options">ffmpeg options.</param>
public sealed partial class FfmpegCapabilities(IProcessRunner runner, IOptions<FfmpegOptions> options) : IFfmpegCapabilities
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private FfmpegCapabilityReport? _report;

    /// <inheritdoc />
    public async Task<FfmpegCapabilityReport> GetAsync(CancellationToken ct = default)
    {
        if (_report is not null)
        {
            return _report;
        }

        await _lock.WaitAsync(ct);
        try
        {
            if (_report is not null)
            {
                return _report;
            }

            var result = await runner.RunAsync(
                options.Value.Path,
                ["-hide_banner", "-encoders"],
                TimeSpan.FromMilliseconds(options.Value.ProbeTimeoutMs),
                ct);
            _report = Evaluate(options.Value.Path, options.Value.VideoEncoder, result);
            return _report;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Builds a report from <c>ffmpeg -encoders</c> output.
    /// </summary>
    /// <param name="ffmpegPath">The configured ffmpeg path (for messages).</param>
    /// <param name="videoEncoder">The configured video encoder.</param>
    /// <param name="result">The process result.</param>
    /// <returns>The report.</returns>
    public static FfmpegCapabilityReport Evaluate(string ffmpegPath, string videoEncoder, ProcessResult result)
    {
        var encoder = videoEncoder.Trim().ToLowerInvariant();
        var encoders = ParseEncoders(result.StandardOutput);
        if (result.ExitCode != 0 || encoders.Count == 0)
        {
            return new FfmpegCapabilityReport(false, encoder, false, false, [],
                $"Could not run ffmpeg at '{ffmpegPath}'. Install ffmpeg or set Ffmpeg:Path. {result.StandardError}".Trim());
        }

        var h264 = encoders
            .Where(e => e.Name == "libx264" || e.Name.StartsWith("h264_", StringComparison.Ordinal))
            .Select(e => e.Name)
            .ToList();
        var hasVideo = encoders.Any(e => e.Name == encoder);
        var hasOpus = encoders.Any(e => e.Name == "libopus");

        string? problem = null;
        if (!hasVideo)
        {
            problem = $"This ffmpeg build has no '{encoder}' encoder. "
                + (h264.Count > 0 ? $"Set Ffmpeg:VideoEncoder to one of: {string.Join(", ", h264)}, or " : string.Empty)
                + "install a full (GPL) ffmpeg build that includes libx264.";
        }
        else if (!hasOpus)
        {
            problem = "This ffmpeg build has no libopus encoder; device audio will not work.";
        }

        return new FfmpegCapabilityReport(true, encoder, hasVideo, hasOpus, h264, problem);
    }

    private static List<(char Kind, string Name)> ParseEncoders(string output) =>
        output.Split('\n')
            .Select(line => EncoderLine().Match(line))
            .Where(m => m.Success)
            .Select(m => (m.Groups["kind"].Value[0], m.Groups["name"].Value))
            .ToList();

    // e.g. " V....D libx264              libx264 H.264 / AVC / MPEG-4 AVC"
    [GeneratedRegex(@"^\s*(?<kind>[VAS])[A-Z.]{5}\s+(?<name>\S+)\s")]
    private static partial Regex EncoderLine();
}

/// <summary>
/// Logs ffmpeg problems at startup so a missing encoder is obvious before anyone presses Watch.
/// </summary>
/// <param name="capabilities">Capability checker.</param>
/// <param name="logger">Logger.</param>
public sealed class FfmpegStartupCheck(IFfmpegCapabilities capabilities, ILogger<FfmpegStartupCheck> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var report = await capabilities.GetAsync(stoppingToken);
        if (report.Problem is null)
        {
            logger.LogInformation(
                "ffmpeg OK: video encoder {Encoder}; H.264 encoders available: {Encoders}",
                report.VideoEncoder,
                string.Join(", ", report.AvailableH264Encoders));
        }
        else if (!report.HasVideoEncoder)
        {
            logger.LogError("ffmpeg problem: {Problem}", report.Problem);
        }
        else
        {
            logger.LogWarning("ffmpeg problem: {Problem}", report.Problem);
        }
    }
}
