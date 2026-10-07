using System.Globalization;
using SixBench.Common.Enums;
using SixBench.Common.Options;
using SixBench.Services.Capture;

namespace SixBench.Services.Ffmpeg;

/// <summary>
/// Builds ffmpeg argument lists for capture, per platform and encoder.
/// </summary>
public static class FfmpegArgsBuilder
{
    private static readonly string[] CommonPrefix = ["-hide_banner", "-loglevel", "warning", "-nostdin"];

    /// <summary>
    /// Arguments that capture video and write raw H.264 Annex-B (with AUD NALs) to stdout.
    /// </summary>
    /// <param name="source">The capture source.</param>
    /// <param name="options">ffmpeg options.</param>
    /// <returns>The argument list.</returns>
    public static IReadOnlyList<string> BuildVideo(CaptureSource source, FfmpegOptions options)
    {
        var args = new List<string>(CommonPrefix);
        var encoder = options.VideoEncoder.Trim().ToLowerInvariant();

        if (encoder == "h264_vaapi")
        {
            args.AddRange(["-vaapi_device", "/dev/dri/renderD128"]);
        }

        // Skip stream probing: live devices otherwise stall ~5s estimating the frame rate, then burst a backlog.
        args.AddRange(["-fflags", "nobuffer", "-flags", "low_delay", "-probesize", "32", "-analyzeduration", "0",
            "-thread_queue_size", "512"]);
        AddVideoInput(args, source);

        var fps = source.FrameRate > 0 ? source.FrameRate : 30;
        var gop = Math.Max(1, (int)Math.Round(fps * Math.Max(0.1, options.GopSeconds)));
        var gopText = gop.ToString(CultureInfo.InvariantCulture);

        // Constant output rate: gives the encoder a correct time base (and level) even when probing is skipped.
        args.AddRange(["-an", "-r", (source.FrameRate > 0 ? source.FrameRate : 30).ToString(CultureInfo.InvariantCulture)]);
        switch (encoder)
        {
            case "h264_videotoolbox":
                args.AddRange(["-c:v", "h264_videotoolbox", "-realtime", "1", "-allow_sw", "1",
                    "-profile:v", "baseline", "-pix_fmt", "yuv420p"]);
                break;
            case "h264_nvenc":
                args.AddRange(["-c:v", "h264_nvenc", "-preset", "p1", "-tune", "ull", "-zerolatency", "1",
                    "-profile:v", "baseline", "-pix_fmt", "yuv420p"]);
                break;
            case "h264_qsv":
                args.AddRange(["-c:v", "h264_qsv", "-preset", "veryfast", "-profile:v", "baseline",
                    "-pix_fmt", "nv12"]);
                break;
            case "h264_vaapi":
                args.AddRange(["-vf", "format=nv12,hwupload", "-c:v", "h264_vaapi", "-profile:v", "constrained_baseline"]);
                break;
            default:
                args.AddRange(["-c:v", "libx264", "-preset", "ultrafast", "-tune", "zerolatency",
                    "-profile:v", "baseline", "-pix_fmt", "yuv420p", "-sc_threshold", "0"]);
                break;
        }

        args.AddRange(["-g", gopText, "-keyint_min", gopText, "-bf", "0"]);

        if (!string.IsNullOrWhiteSpace(options.VideoBitrate))
        {
            var rate = options.VideoBitrate.Trim();
            args.AddRange(["-b:v", rate, "-maxrate", rate, "-bufsize", rate]);
        }

        AddExtraArgs(args, options.ExtraEncoderArgs);

        args.AddRange(["-bsf:v", "h264_metadata=aud=insert", "-flush_packets", "1", "-f", "h264", "pipe:1"]);
        return args;
    }

    /// <summary>
    /// Arguments that capture audio and write 48 kHz stereo Opus (20 ms packets) in Ogg to stdout.
    /// </summary>
    /// <param name="source">The capture source; must have an <see cref="CaptureSource.AudioInput"/>.</param>
    /// <param name="options">ffmpeg options.</param>
    /// <param name="audioOptions">Audio options.</param>
    /// <returns>The argument list.</returns>
    /// <exception cref="InvalidOperationException">The source has no audio input.</exception>
    public static IReadOnlyList<string> BuildAudio(CaptureSource source, FfmpegOptions options, AudioOptions audioOptions)
    {
        if (string.IsNullOrWhiteSpace(source.AudioInput))
        {
            throw new InvalidOperationException($"Capture device '{source.Name}' has no audio input.");
        }

        var args = new List<string>(CommonPrefix);
        args.AddRange(["-fflags", "nobuffer", "-probesize", "32", "-analyzeduration", "0", "-thread_queue_size", "512"]);

        switch (source.Platform)
        {
            case HostPlatform.MacOS:
                args.AddRange(["-f", "avfoundation", "-i", $"none:{source.AudioInput}"]);
                break;
            case HostPlatform.Windows:
                args.AddRange(["-f", "dshow", "-audio_buffer_size", "20", "-i", $"audio={source.AudioInput}"]);
                break;
            case HostPlatform.Linux:
                args.AddRange(["-f", "alsa", "-i", source.AudioInput]);
                break;
            default:
                throw new PlatformNotSupportedException("Audio capture is not supported on this platform.");
        }

        args.AddRange(["-vn", "-c:a", "libopus", "-b:a", audioOptions.DeviceBitrate, "-ar", "48000", "-ac", "2",
            "-application", "lowdelay", "-frame_duration", "20",
            "-flush_packets", "1", "-page_duration", "20000", "-f", "ogg", "pipe:1"]);
        return args;
    }

    private static void AddVideoInput(List<string> args, CaptureSource source)
    {
        var fps = (source.FrameRate > 0 ? source.FrameRate : 30).ToString(CultureInfo.InvariantCulture);
        switch (source.Platform)
        {
            case HostPlatform.MacOS:
                args.AddRange(["-f", "avfoundation", "-framerate", fps]);
                AddIfSet(args, "-video_size", source.VideoSize);
                AddIfSet(args, "-pixel_format", source.PixelFormat);
                args.AddRange(["-i", $"{source.VideoInput}:none"]);
                break;
            case HostPlatform.Windows:
                args.AddRange(["-f", "dshow", "-rtbufsize", "256M", "-framerate", fps]);
                AddIfSet(args, "-video_size", source.VideoSize);
                AddIfSet(args, "-pixel_format", source.PixelFormat);
                args.AddRange(["-i", $"video={source.VideoInput}"]);
                break;
            case HostPlatform.Linux:
                args.AddRange(["-f", "v4l2", "-framerate", fps]);
                AddIfSet(args, "-video_size", source.VideoSize);
                // v4l2 uses -input_format for both raw (yuyv422) and compressed (mjpeg) formats.
                AddIfSet(args, "-input_format", source.PixelFormat);
                args.AddRange(["-i", source.VideoInput]);
                break;
            default:
                throw new PlatformNotSupportedException("Video capture is not supported on this platform.");
        }
    }

    private static void AddIfSet(List<string> args, string flag, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            args.AddRange([flag, value.Trim()]);
        }
    }

    private static void AddExtraArgs(List<string> args, string? extra)
    {
        if (string.IsNullOrWhiteSpace(extra))
        {
            return;
        }

        args.AddRange(extra.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
}
