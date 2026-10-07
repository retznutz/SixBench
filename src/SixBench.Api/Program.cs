using System.Text.Json.Serialization;
using Asp.Versioning;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using SixBench.Api.Infrastructure;
using SixBench.Data;
using SixBench.Services;
using Swashbuckle.AspNetCore.SwaggerGen;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Logging: Serilog owns the pipeline; events are also forwarded to Microsoft.Extensions.Logging providers
    // so Application Insights (OpenTelemetry-based in v3) receives them.
    builder.Logging.ClearProviders();
    builder.Services.AddSerilog(
        (services, lc) => lc
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext(),
        writeToProviders: true);

    var appInsights = builder.Configuration["ApplicationInsights:ConnectionString"];
    if (!string.IsNullOrWhiteSpace(appInsights))
    {
        builder.Services.AddApplicationInsightsTelemetry(o => o.ConnectionString = appInsights);
    }

    builder.Services.AddSixBenchData(builder.Configuration, builder.Environment.ContentRootPath);
    builder.Services.AddSixBenchServices(builder.Configuration);

    builder.Services
        .AddControllers()
        .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<ApiExceptionHandler>();
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<SixBenchDbContext>()
        .AddCheck<FfmpegHealthCheck>("ffmpeg");

    builder.Services
        .AddApiVersioning(o =>
        {
            o.DefaultApiVersion = new ApiVersion(1, 0);
            o.ReportApiVersions = true;
            o.ApiVersionReader = new UrlSegmentApiVersionReader();
        })
        .AddMvc()
        .AddApiExplorer(o =>
        {
            o.GroupNameFormat = "'v'VVV";
            o.SubstituteApiVersionInUrl = true;
        });
    builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
    builder.Services.AddSwaggerGen();

    var devOrigins = builder.Configuration.GetSection("Cors:DevOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(o => o.AddPolicy("dev", p => p.WithOrigins(devOrigins).AllowAnyHeader().AllowAnyMethod()));

    var app = builder.Build();

    await using (var scope = app.Services.CreateAsyncScope())
    {
        await scope.ServiceProvider.GetRequiredService<SixBenchDbContext>().Database.MigrateAsync();
    }

    app.UseExceptionHandler();
    app.UseSerilogRequestLogging(o =>
    {
        // Stream sockets live for minutes; log them at debug to keep request logs readable.
        o.GetLevel = (ctx, _, ex) => ex is not null || ctx.Response.StatusCode >= 500
            ? Serilog.Events.LogEventLevel.Error
            : ctx.WebSockets.IsWebSocketRequest ? Serilog.Events.LogEventLevel.Debug : Serilog.Events.LogEventLevel.Information;
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseCors("dev");
    }
    else
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    app.UseSwagger();
    app.UseSwaggerUI(o =>
    {
        foreach (var description in app.DescribeApiVersions())
        {
            o.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", $"SixBench {description.GroupName}");
        }
    });

    app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(15) });
    app.UseDefaultFiles();
    app.UseStaticFiles();

    app.MapControllers();
    app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = HealthResponseWriter.WriteAsync });

    // SPA fallback for client-side routes; unknown API paths stay 404.
    app.MapFallback("api/{**path}", () => Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Not found"));
    if (File.Exists(Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "index.html")))
    {
        app.MapFallbackToFile("index.html");
    }

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "SixBench terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>
/// Entry point (exposed for integration tests).
/// </summary>
public partial class Program;
