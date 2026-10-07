using SixBench.Common.Enums;

namespace SixBench.Common.Dtos;

/// <summary>
/// Request body to send a remote-control key to a Roku.
/// </summary>
public sealed class KeyCommandRequest
{
    /// <summary>The key to send.</summary>
    public RokuKey Key { get; set; }

    /// <summary>Press (default), hold, or release.</summary>
    public KeyAction Action { get; set; } = KeyAction.Press;
}
