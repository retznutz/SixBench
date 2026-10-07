using System.Diagnostics;
using Microsoft.Extensions.Options;
using SixBench.Common.Options;

namespace SixBench.Api.Infrastructure;

/// <summary>
/// Restarts the server: starts a new SixBench process (unless <c>Server:SelfRestart</c> is false) and stops this one.
/// The new process waits for this one to exit before it binds the port.
/// </summary>
/// <param name="lifetime">Application lifetime.</param>
/// <param name="options">Server options.</param>
/// <param name="logger">Logger.</param>
public sealed class ServerRestarter(IHostApplicationLifetime lifetime, IOptions<ServerOptions> options, ILogger<ServerRestarter> logger)
{
    /// <summary>Environment variable that tells a relaunched process which process to wait for.</summary>
    public const string WaitForPidVariable = "SIXBENCH_WAIT_FOR_PID";

    /// <summary>
    /// Restarts. Returns immediately; shutdown starts half a second later so the HTTP response can be sent.
    /// </summary>
    /// <returns>True if a new process was started; false if a service manager is expected to restart SixBench.</returns>
    public bool Restart()
    {
        var relaunched = false;
        if (options.Value.SelfRestart && Environment.ProcessPath is { } processPath)
        {
            var (fileName, arguments) = BuildRelaunchCommand(processPath, Environment.GetCommandLineArgs());
            var start = new ProcessStartInfo(fileName)
            {
                UseShellExecute = false,
                WorkingDirectory = Environment.CurrentDirectory,
            };
            foreach (var argument in arguments)
            {
                start.ArgumentList.Add(argument);
            }

            start.Environment[WaitForPidVariable] = Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture);
            using var process = Process.Start(start);
            relaunched = process is not null;
            logger.LogWarning("Restarting: started {File} (pid {Pid})", fileName, process?.Id);
        }
        else
        {
            logger.LogWarning("Stopping for restart; a service manager is expected to start SixBench again");
        }

        _ = Task.Run(async () =>
        {
            await Task.Delay(500);
            lifetime.StopApplication();
        });
        return relaunched;
    }

    /// <summary>
    /// The command that starts this app again. Under the <c>dotnet</c> host the app's .dll (argument 0) must be passed;
    /// the native launcher and single-file builds start directly.
    /// </summary>
    /// <param name="processPath"><see cref="Environment.ProcessPath"/>.</param>
    /// <param name="commandLineArgs"><see cref="Environment.GetCommandLineArgs"/> (argument 0 is the app's .dll).</param>
    /// <returns>Executable and arguments.</returns>
    public static (string FileName, IReadOnlyList<string> Arguments) BuildRelaunchCommand(string processPath, IReadOnlyList<string> commandLineArgs)
    {
        var isDotnetHost = Path.GetFileNameWithoutExtension(processPath).Equals("dotnet", StringComparison.OrdinalIgnoreCase);
        var arguments = isDotnetHost ? commandLineArgs.ToList() : commandLineArgs.Skip(1).ToList();
        return (processPath, arguments);
    }

    /// <summary>
    /// Called first thing at startup: if this process was started by <see cref="Restart"/>, waits (up to 30 s)
    /// for the previous process to exit so the port is free.
    /// </summary>
    public static void WaitForPreviousProcess()
    {
        if (!int.TryParse(Environment.GetEnvironmentVariable(WaitForPidVariable), out var pid))
        {
            return;
        }

        Environment.SetEnvironmentVariable(WaitForPidVariable, null);
        try
        {
            using var previous = Process.GetProcessById(pid);
            previous.WaitForExit(TimeSpan.FromSeconds(30));
        }
        catch (ArgumentException)
        {
            // Already gone.
        }
    }
}
