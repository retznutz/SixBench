using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SixBench.Common.Enums;
using SixBench.Common.Options;
using SixBench.Services.Capture;
using SixBench.Services.Ffmpeg;
using SixBench.Services.Links;
using SixBench.Services.Processes;
using SixBench.Services.Roku;
using SixBench.Services.Streaming;

namespace SixBench.Services;

/// <summary>
/// Registers the SixBench business services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds options, capture, streaming and Roku services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">App configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddSixBenchServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<FfmpegOptions>().Bind(configuration.GetSection(FfmpegOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Path), "Ffmpeg:Path is required.")
            .Validate(o => o.GopSeconds > 0, "Ffmpeg:GopSeconds must be positive.")
            .ValidateOnStart();
        services.AddOptions<StreamingOptions>().Bind(configuration.GetSection(StreamingOptions.SectionName))
            .Validate(o => o.SubscriberQueueFrames >= 8, "Streaming:SubscriberQueueFrames must be at least 8.")
            .ValidateOnStart();
        services.AddOptions<AudioOptions>().Bind(configuration.GetSection(AudioOptions.SectionName)).ValidateOnStart();
        services.AddOptions<RokuOptions>().Bind(configuration.GetSection(RokuOptions.SectionName))
            .Validate(o => o.DiscoveryTimeoutMs is > 0 and <= 30000, "Roku:DiscoveryTimeoutMs must be 1-30000.")
            .ValidateOnStart();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IProcessRunner, ProcessRunner>();
        services.AddSingleton<IFfmpegCapabilities, FfmpegCapabilities>();
        services.AddHostedService<FfmpegStartupCheck>();

        // Capture
        switch (HostPlatformDetector.Current)
        {
            case HostPlatform.MacOS:
                services.AddSingleton<ICaptureDeviceEnumerator, AvFoundationDeviceEnumerator>();
                break;
            case HostPlatform.Windows:
                services.AddSingleton<ICaptureDeviceEnumerator, DirectShowDeviceEnumerator>();
                break;
            case HostPlatform.Linux:
                services.AddSingleton<ICaptureDeviceEnumerator, V4l2DeviceEnumerator>();
                break;
            default:
                services.AddSingleton<ICaptureDeviceEnumerator, UnsupportedDeviceEnumerator>();
                break;
        }

        services.AddSingleton<IDeviceInventoryProvider, DeviceInventoryProvider>();
        services.AddSingleton<IAudioPolicy, AudioPolicy>();
        services.AddScoped<ICaptureDeviceService, CaptureDeviceService>();
        services.AddScoped<IEncoderLinkService, EncoderLinkService>();

        // Streaming
        services.AddSingleton<IMediaSourceFactory, FfmpegMediaSourceFactory>();
        services.AddSingleton<CaptureSessionManager>();
        services.AddSingleton<ICaptureSessionManager>(sp => sp.GetRequiredService<CaptureSessionManager>());
        services.AddHostedService(sp => sp.GetRequiredService<CaptureSessionManager>());
        services.AddSingleton<IStreamSocketHandler, StreamSocketHandler>();

        // Roku
        services.AddHttpClient<IRokuEcpClient, RokuEcpClient>((sp, http) =>
        {
            http.Timeout = TimeSpan.FromMilliseconds(sp.GetRequiredService<IOptions<RokuOptions>>().Value.RequestTimeoutMs);
        });
        services.AddTransient<IRokuDiscoveryService, RokuDiscoveryService>();
        services.AddScoped<IRokuDeviceService, RokuDeviceService>();
        services.AddScoped<IRokuControlService, RokuControlService>();

        return services;
    }
}
