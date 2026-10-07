using SixBench.Common.Enums;
using SixBench.Common.Options;
using SixBench.Services.Capture;
using SixBench.Services.Ffmpeg;

namespace SixBench.Tests.Capture;

public class FfmpegArgsBuilderTests
{
    private static CaptureSource Source(HostPlatform platform, string video, string? audio = "mic", double fps = 30,
        string? size = null, string? pixfmt = null) =>
        new("id", "Name", platform, video, audio, fps, size, pixfmt, new AudioGrant(true, false));

    private static string Join(IReadOnlyList<string> args) => string.Join(' ', args);

    [Fact]
    public void Mac_video_uses_device_name_with_no_audio_and_annexb_output()
    {
        var args = FfmpegArgsBuilder.BuildVideo(Source(HostPlatform.MacOS, "USB3.0 Capture"), new FfmpegOptions());
        var text = Join(args);

        Assert.Contains("-f avfoundation -framerate 30 -i", text);
        Assert.Equal("USB3.0 Capture:none", args[args.ToList().IndexOf("-i") + 1]);
        Assert.Contains("-c:v libx264 -preset ultrafast -tune zerolatency -profile:v baseline -pix_fmt yuv420p", text);
        Assert.Contains("-g 30 -keyint_min 30 -bf 0", text);
        Assert.Contains("-probesize 32 -analyzeduration 0", text);
        Assert.EndsWith("-bsf:v h264_metadata=aud=insert -flush_packets 1 -f h264 pipe:1", text);
    }

    [Fact]
    public void Capture_overrides_and_gop_follow_the_source()
    {
        var options = new FfmpegOptions { GopSeconds = 2, VideoEncoder = "h264_videotoolbox", VideoBitrate = null };
        var text = Join(FfmpegArgsBuilder.BuildVideo(Source(HostPlatform.MacOS, "cam", fps: 59.94, size: "1920x1080", pixfmt: "uyvy422"), options));

        Assert.Contains("-framerate 59.94 -video_size 1920x1080 -pixel_format uyvy422", text);
        Assert.Contains("-c:v h264_videotoolbox -realtime 1", text);
        Assert.Contains("-g 120", text);
        Assert.DoesNotContain("-b:v", text);
    }

    [Fact]
    public void Windows_uses_dshow_video_prefix()
    {
        var args = FfmpegArgsBuilder.BuildVideo(Source(HostPlatform.Windows, "@device_pnp_x"), new FfmpegOptions());

        Assert.Equal("video=@device_pnp_x", args[args.ToList().IndexOf("-i") + 1]);
        Assert.Contains("-rtbufsize", args);
    }

    [Theory]
    [InlineData("h264_amf", "-c:v h264_amf -usage ultralowlatency")]
    [InlineData("h264_nvenc", "-c:v h264_nvenc -preset p1 -tune ull")]
    [InlineData("h264_mf", "-c:v h264_mf -pix_fmt yuv420p")]
    public void Encoders_get_matching_arguments(string encoder, string expected)
    {
        var text = Join(FfmpegArgsBuilder.BuildVideo(Source(HostPlatform.Windows, "cam"), new FfmpegOptions { VideoEncoder = encoder }));

        Assert.Contains(expected, text);
        Assert.DoesNotContain("libx264", text);
    }

    [Fact]
    public void Linux_uses_v4l2_input_format_for_pixel_format()
    {
        var text = Join(FfmpegArgsBuilder.BuildVideo(Source(HostPlatform.Linux, "/dev/v4l/by-id/usb-x-video-index0", pixfmt: "mjpeg"), new FfmpegOptions()));

        Assert.Contains("-f v4l2 -framerate 30 -input_format mjpeg -i /dev/v4l/by-id/usb-x-video-index0", text);
    }

    [Theory]
    [InlineData(HostPlatform.MacOS, "-f avfoundation -i none:mic")]
    [InlineData(HostPlatform.Windows, "-i audio=mic")]
    [InlineData(HostPlatform.Linux, "-f alsa -i mic")]
    public void Audio_is_48k_stereo_opus_in_ogg(HostPlatform platform, string input)
    {
        var text = Join(FfmpegArgsBuilder.BuildAudio(Source(platform, "cam"), new FfmpegOptions(), new AudioOptions { DeviceBitrate = "96k" }));

        Assert.Contains(input, text);
        Assert.Contains("-c:a libopus -b:a 96k -ar 48000 -ac 2 -application lowdelay -frame_duration 20", text);
        Assert.EndsWith("-f ogg pipe:1", text);
    }

    [Fact]
    public void Audio_without_input_throws() =>
        Assert.Throws<InvalidOperationException>(() =>
            FfmpegArgsBuilder.BuildAudio(Source(HostPlatform.MacOS, "cam", audio: null), new FfmpegOptions(), new AudioOptions()));
}
