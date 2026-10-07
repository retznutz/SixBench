using SixBench.Common.Enums;

namespace SixBench.Services.Capture;

/// <summary>
/// Detects the current <see cref="HostPlatform"/>.
/// </summary>
public static class HostPlatformDetector
{
    /// <summary>The platform this process is running on.</summary>
    public static HostPlatform Current { get; } =
        OperatingSystem.IsMacOS() ? HostPlatform.MacOS
        : OperatingSystem.IsWindows() ? HostPlatform.Windows
        : OperatingSystem.IsLinux() ? HostPlatform.Linux
        : HostPlatform.Unknown;
}
