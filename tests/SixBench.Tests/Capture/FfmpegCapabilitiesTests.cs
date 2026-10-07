using SixBench.Services.Ffmpeg;
using SixBench.Services.Processes;

namespace SixBench.Tests.Capture;

public class FfmpegCapabilitiesTests
{
    private const string LgplBuild = """
        Encoders:
         V..... = Video
         A..... = Audio
         ------
         V....D h264_amf             AMD AMF H.264 Encoder (codec h264)
         V....D h264_mf              H264 via MediaFoundation (codec h264)
         V....D h264_nvenc           NVIDIA NVENC H.264 encoder (codec h264)
         V....D h264_qsv             H.264 / AVC / MPEG-4 AVC / MPEG-4 part 10 (Intel Quick Sync Video acceleration) (codec h264)
         A....D libopus              libopus Opus (codec opus)
        """;

    [Fact]
    public void Missing_libx264_suggests_available_hardware_encoders()
    {
        var report = FfmpegCapabilities.Evaluate("ffmpeg", "libx264", new ProcessResult(0, LgplBuild, string.Empty));

        Assert.True(report.Runnable);
        Assert.False(report.HasVideoEncoder);
        Assert.True(report.HasOpus);
        Assert.Equal(["h264_amf", "h264_mf", "h264_nvenc", "h264_qsv"], report.AvailableH264Encoders);
        Assert.Contains("h264_nvenc", report.Problem);
        Assert.Contains("libx264", report.Problem);
    }

    [Fact]
    public void Configured_hardware_encoder_is_accepted()
    {
        var report = FfmpegCapabilities.Evaluate("ffmpeg", "H264_NVENC", new ProcessResult(0, LgplBuild, string.Empty));

        Assert.True(report.HasVideoEncoder);
        Assert.Null(report.Problem);
    }

    [Fact]
    public void Unrunnable_ffmpeg_is_reported()
    {
        var report = FfmpegCapabilities.Evaluate("C:/nope/ffmpeg.exe", "libx264", new ProcessResult(-1, string.Empty, "not found"));

        Assert.False(report.Runnable);
        Assert.Contains("C:/nope/ffmpeg.exe", report.Problem);
    }
}
