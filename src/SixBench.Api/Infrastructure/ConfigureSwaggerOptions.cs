using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SixBench.Api.Infrastructure;

/// <summary>
/// Creates one Swagger document per API version.
/// </summary>
/// <param name="provider">API version description provider.</param>
public sealed class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) : IConfigureOptions<SwaggerGenOptions>
{
    /// <inheritdoc />
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = "SixBench API",
                Version = description.ApiVersion.ToString(),
                Description = "Stream HDMI capture encoders to the browser and control the attached Roku. " +
                    "The video stream WebSocket is at /api/v1/streams/{id}/ws (see README).",
            });
        }

        foreach (var xml in Directory.EnumerateFiles(AppContext.BaseDirectory, "SixBench.*.xml"))
        {
            options.IncludeXmlComments(xml, includeControllerXmlComments: true);
        }
    }
}
