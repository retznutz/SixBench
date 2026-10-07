namespace SixBench.Services.Processes;

/// <summary>
/// Runs short-lived external commands and captures their output.
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Runs a command to completion.
    /// </summary>
    /// <param name="fileName">Executable to run.</param>
    /// <param name="arguments">Arguments (passed without shell interpretation).</param>
    /// <param name="timeout">Maximum run time; the process is killed when exceeded.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The exit code and captured output.</returns>
    Task<ProcessResult> RunAsync(string fileName, IReadOnlyList<string> arguments, TimeSpan timeout, CancellationToken ct = default);
}

/// <summary>
/// Output of a completed process.
/// </summary>
/// <param name="ExitCode">Process exit code (-1 if it could not be started or timed out).</param>
/// <param name="StandardOutput">Captured stdout.</param>
/// <param name="StandardError">Captured stderr.</param>
public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
