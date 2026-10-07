namespace SixBench.Common.Enums;

/// <summary>
/// How a key is sent to the Roku.
/// </summary>
public enum KeyAction
{
    /// <summary>Press and release (ECP <c>/keypress</c>).</summary>
    Press,
    /// <summary>Press and hold (ECP <c>/keydown</c>).</summary>
    Down,
    /// <summary>Release a held key (ECP <c>/keyup</c>).</summary>
    Up,
}
