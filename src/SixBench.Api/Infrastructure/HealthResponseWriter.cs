using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SixBench.Api.Infrastructure;

/// <summary>
/// Writes health results as JSON with one entry per check, so problems such as a missing ffmpeg encoder are visible.
/// </summary>
public static class HealthResponseWriter
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>
    /// Writes the health report.
    /// </summary>
    /// <param name="context">HTTP context.</param>
    /// <param name="report">Health report.</param>
    /// <returns>A task.</returns>
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var body = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.ToDictionary(
                e => e.Key,
                e => new { status = e.Value.Status.ToString(), description = e.Value.Description, data = e.Value.Data }),
        };
        return context.Response.WriteAsync(JsonSerializer.Serialize(body, Options));
    }
}
