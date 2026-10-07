using Microsoft.Extensions.Diagnostics.HealthChecks;
using SixBench.Services.Ffmpeg;

namespace SixBench.Api.Infrastructure;

/// <summary>
/// Reports whether ffmpeg is installed with the configured encoder.
/// Unhealthy when ffmpeg or the video encoder is missing; degraded when only libopus is missing.
/// </summary>
/// <param name="capabilities">Capability checker.</param>
public sealed class FfmpegHealthCheck(IFfmpegCapabilities capabilities) : IHealthCheck
{
    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var report = await capabilities.GetAsync(cancellationToken);
        var data = new Dictionary<string, object>
        {
            ["videoEncoder"] = report.VideoEncoder,
            ["availableH264Encoders"] = report.AvailableH264Encoders,
            ["libopus"] = report.HasOpus,
        };

        return report switch
        {
            { Runnable: false } or { HasVideoEncoder: false } => HealthCheckResult.Unhealthy(report.Problem, data: data),
            { Problem: not null } => HealthCheckResult.Degraded(report.Problem, data: data),
            _ => HealthCheckResult.Healthy("ffmpeg OK", data),
        };
    }
}
