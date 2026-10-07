using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace SixBench.Services.Ffmpeg;

/// <summary>
/// A long-running ffmpeg child process whose stdout carries media data.
/// stderr is forwarded to the logger and the most recent lines are kept for error reporting.
/// </summary>
public sealed class FfmpegProcess : IAsyncDisposable
{
    private const int MaxErrorLines = 20;

    private readonly Process _process;
    private readonly ILogger _logger;
    private readonly string _name;
    private readonly Queue<string> _recentErrors = new();
    private readonly object _errorsLock = new();
    private int _disposed;

    private FfmpegProcess(Process process, ILogger logger, string name)
    {
        _process = process;
        _logger = logger;
        _name = name;
    }

    /// <summary>Process stdout as a raw byte stream.</summary>
    public Stream Output => _process.StandardOutput.BaseStream;

    /// <summary>Operating-system process id.</summary>
    public int ProcessId => _process.Id;

    /// <summary>The last lines ffmpeg wrote to stderr.</summary>
    public string RecentErrors
    {
        get
        {
            lock (_errorsLock)
            {
                return string.Join('\n', _recentErrors);
            }
        }
    }

    /// <summary>
    /// Starts ffmpeg.
    /// </summary>
    /// <param name="ffmpegPath">Executable path.</param>
    /// <param name="arguments">Arguments (no shell quoting).</param>
    /// <param name="logger">Logger for stderr.</param>
    /// <param name="name">Short name used in log messages, e.g. <c>video:FaceTime HD Camera</c>.</param>
    /// <returns>The running process.</returns>
    /// <exception cref="InvalidOperationException">ffmpeg could not be started.</exception>
    public static FfmpegProcess Start(string ffmpegPath, IReadOnlyList<string> arguments, ILogger logger, string name)
    {
        var psi = new ProcessStartInfo(ffmpegPath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in arguments)
        {
            psi.ArgumentList.Add(arg);
        }

        var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var wrapper = new FfmpegProcess(process, logger, name);
        process.ErrorDataReceived += wrapper.OnErrorData;

        try
        {
            process.Start();
        }
        catch (Win32Exception ex)
        {
            process.Dispose();
            throw new InvalidOperationException($"Could not start ffmpeg at '{ffmpegPath}': {ex.Message}", ex);
        }

        process.BeginErrorReadLine();
        logger.LogInformation(
            "Started ffmpeg {Name} (pid {Pid}): {Args}",
            name,
            process.Id,
            string.Join(' ', arguments.Select(a => a.Contains(' ') ? $"\"{a}\"" : a)));
        return wrapper;
    }

    /// <summary>
    /// Waits for the process to exit.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The exit code.</returns>
    public async Task<int> WaitForExitAsync(CancellationToken ct = default)
    {
        await _process.WaitForExitAsync(ct);
        return _process.ExitCode;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _process.WaitForExitAsync(cts.Token);
            }

            _logger.LogInformation("Stopped ffmpeg {Name} (exit {ExitCode})", _name, _process.ExitCode);
        }
        catch (Exception ex) when (ex is InvalidOperationException or OperationCanceledException or Win32Exception)
        {
            _logger.LogWarning(ex, "Error stopping ffmpeg {Name}", _name);
        }
        finally
        {
            _process.ErrorDataReceived -= OnErrorData;
            _process.Dispose();
        }
    }

    private void OnErrorData(object sender, DataReceivedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Data))
        {
            return;
        }

        lock (_errorsLock)
        {
            _recentErrors.Enqueue(e.Data);
            while (_recentErrors.Count > MaxErrorLines)
            {
                _recentErrors.Dequeue();
            }
        }

        _logger.LogDebug("ffmpeg {Name}: {Line}", _name, e.Data);
    }
}
