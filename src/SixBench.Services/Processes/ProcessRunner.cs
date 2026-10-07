using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace SixBench.Services.Processes;

/// <summary>
/// Default <see cref="IProcessRunner"/> backed by <see cref="Process"/>.
/// </summary>
/// <param name="logger">Logger.</param>
public sealed class ProcessRunner(ILogger<ProcessRunner> logger) : IProcessRunner
{
    /// <inheritdoc />
    public async Task<ProcessResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo(fileName)
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

        using var process = new Process { StartInfo = psi };
        try
        {
            process.Start();
        }
        catch (Win32Exception ex)
        {
            logger.LogWarning(ex, "Could not start {FileName}", fileName);
            return new ProcessResult(-1, string.Empty, ex.Message);
        }

        process.StandardInput.Close();
        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeout);
        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            if (ct.IsCancellationRequested)
            {
                throw;
            }

            logger.LogWarning("{FileName} timed out after {Timeout}", fileName, timeout);
            return new ProcessResult(-1, string.Empty, $"Timed out after {timeout}.");
        }

        return new ProcessResult(process.ExitCode, await stdoutTask, await stderrTask);
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // Already exited.
        }
    }
}
