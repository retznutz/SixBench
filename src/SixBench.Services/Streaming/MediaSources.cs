using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixBench.Common.Options;
using SixBench.Services.Capture;
using SixBench.Services.Ffmpeg;

namespace SixBench.Services.Streaming;

/// <summary>
/// A live source of H.264 access units.
/// </summary>
public interface IVideoSource : IAsyncDisposable
{
    /// <summary>Why the source stopped, when it stopped on its own.</summary>
    string? FailureReason { get; }

    /// <summary>
    /// Starts capture and yields access units until the source ends or <paramref name="ct"/> is cancelled.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Access units.</returns>
    IAsyncEnumerable<AccessUnit> ReadAsync(CancellationToken ct);
}

/// <summary>
/// A live source of Opus packets (48 kHz stereo, 20 ms).
/// </summary>
public interface IAudioSource : IAsyncDisposable
{
    /// <summary>Why the source stopped, when it stopped on its own.</summary>
    string? FailureReason { get; }

    /// <summary>
    /// Starts capture and yields Opus packets until the source ends or <paramref name="ct"/> is cancelled.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Opus packets.</returns>
    IAsyncEnumerable<byte[]> ReadAsync(CancellationToken ct);
}

/// <summary>
/// Creates media sources for a capture device. Swapped for fakes in tests.
/// </summary>
public interface IMediaSourceFactory
{
    /// <summary>Creates a video source.</summary>
    /// <param name="source">Capture source.</param>
    /// <returns>An unstarted video source.</returns>
    IVideoSource CreateVideo(CaptureSource source);

    /// <summary>Creates an audio source.</summary>
    /// <param name="source">Capture source (must have an audio input).</param>
    /// <returns>An unstarted audio source.</returns>
    IAudioSource CreateAudio(CaptureSource source);
}

/// <summary>
/// ffmpeg-backed <see cref="IMediaSourceFactory"/>.
/// </summary>
/// <param name="ffmpegOptions">ffmpeg options.</param>
/// <param name="audioOptions">Audio options.</param>
/// <param name="loggerFactory">Logger factory.</param>
public sealed class FfmpegMediaSourceFactory(
    IOptions<FfmpegOptions> ffmpegOptions,
    IOptions<AudioOptions> audioOptions,
    ILoggerFactory loggerFactory) : IMediaSourceFactory
{
    /// <inheritdoc />
    public IVideoSource CreateVideo(CaptureSource source) => new FfmpegVideoSource(
        ffmpegOptions.Value.Path,
        FfmpegArgsBuilder.BuildVideo(source, ffmpegOptions.Value),
        $"video:{source.Name}",
        loggerFactory.CreateLogger<FfmpegVideoSource>());

    /// <inheritdoc />
    public IAudioSource CreateAudio(CaptureSource source) => new FfmpegAudioSource(
        ffmpegOptions.Value.Path,
        FfmpegArgsBuilder.BuildAudio(source, ffmpegOptions.Value, audioOptions.Value),
        $"audio:{source.Name}",
        loggerFactory.CreateLogger<FfmpegAudioSource>());
}

/// <summary>
/// Runs ffmpeg and splits its H.264 stdout into access units.
/// </summary>
/// <param name="ffmpegPath">ffmpeg executable.</param>
/// <param name="arguments">ffmpeg arguments.</param>
/// <param name="name">Name for logs.</param>
/// <param name="logger">Logger.</param>
public sealed class FfmpegVideoSource(string ffmpegPath, IReadOnlyList<string> arguments, string name, ILogger logger)
    : IVideoSource
{
    private FfmpegProcess? _process;

    /// <inheritdoc />
    public string? FailureReason { get; private set; }

    /// <inheritdoc />
    public async IAsyncEnumerable<AccessUnit> ReadAsync([EnumeratorCancellation] CancellationToken ct)
    {
        _process = FfmpegProcess.Start(ffmpegPath, arguments, logger, name);
        var parser = new AnnexBParser();
        var buffer = new byte[64 * 1024];
        int read;
        while ((read = await ReadChunkAsync(_process.Output, buffer, ct)) > 0)
        {
            foreach (var unit in parser.Push(buffer.AsSpan(0, read)))
            {
                yield return unit;
            }
        }

        foreach (var unit in parser.Flush())
        {
            yield return unit;
        }

        if (!ct.IsCancellationRequested)
        {
            var exitCode = await _process.WaitForExitAsync(CancellationToken.None);
            FailureReason = FormatFailure(exitCode, _process.RecentErrors);
            logger.LogWarning("ffmpeg {Name} ended: {Reason}", name, FailureReason);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_process is not null)
        {
            await _process.DisposeAsync();
        }
    }

    internal static async Task<int> ReadChunkAsync(Stream stream, byte[] buffer, CancellationToken ct)
    {
        try
        {
            return await stream.ReadAsync(buffer, ct);
        }
        catch (IOException)
        {
            return 0;
        }
        catch (ObjectDisposedException)
        {
            return 0;
        }
    }

    internal static string FormatFailure(int exitCode, string recentErrors) =>
        string.IsNullOrWhiteSpace(recentErrors)
            ? $"ffmpeg exited with code {exitCode}."
            : $"ffmpeg exited with code {exitCode}: {recentErrors}";
}

/// <summary>
/// Runs ffmpeg and extracts Opus packets from its Ogg stdout.
/// </summary>
/// <param name="ffmpegPath">ffmpeg executable.</param>
/// <param name="arguments">ffmpeg arguments.</param>
/// <param name="name">Name for logs.</param>
/// <param name="logger">Logger.</param>
public sealed class FfmpegAudioSource(string ffmpegPath, IReadOnlyList<string> arguments, string name, ILogger logger)
    : IAudioSource
{
    private FfmpegProcess? _process;

    /// <inheritdoc />
    public string? FailureReason { get; private set; }

    /// <inheritdoc />
    public async IAsyncEnumerable<byte[]> ReadAsync([EnumeratorCancellation] CancellationToken ct)
    {
        _process = FfmpegProcess.Start(ffmpegPath, arguments, logger, name);
        var parser = new OggOpusParser();
        var buffer = new byte[16 * 1024];
        int read;
        while ((read = await FfmpegVideoSource.ReadChunkAsync(_process.Output, buffer, ct)) > 0)
        {
            foreach (var packet in parser.Push(buffer.AsSpan(0, read)))
            {
                yield return packet;
            }
        }

        if (!ct.IsCancellationRequested)
        {
            var exitCode = await _process.WaitForExitAsync(CancellationToken.None);
            FailureReason = FfmpegVideoSource.FormatFailure(exitCode, _process.RecentErrors);
            logger.LogWarning("ffmpeg {Name} ended: {Reason}", name, FailureReason);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_process is not null)
        {
            await _process.DisposeAsync();
        }
    }
}
