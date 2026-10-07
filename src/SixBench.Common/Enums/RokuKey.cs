namespace SixBench.Common.Enums;

/// <summary>
/// Keys supported by the Roku External Control Protocol (ECP).
/// The enum member names match the ECP key names exactly, so <c>ToString()</c> yields the wire value.
/// </summary>
public enum RokuKey
{
    /// <summary>Home button.</summary>
    Home,
    /// <summary>Rewind.</summary>
    Rev,
    /// <summary>Fast forward.</summary>
    Fwd,
    /// <summary>Play / pause toggle.</summary>
    Play,
    /// <summary>OK / select.</summary>
    Select,
    /// <summary>D-pad left.</summary>
    Left,
    /// <summary>D-pad right.</summary>
    Right,
    /// <summary>D-pad down.</summary>
    Down,
    /// <summary>D-pad up.</summary>
    Up,
    /// <summary>Back button.</summary>
    Back,
    /// <summary>Instant replay.</summary>
    InstantReplay,
    /// <summary>Info / options (*) button.</summary>
    Info,
    /// <summary>Backspace (on-screen keyboard).</summary>
    Backspace,
    /// <summary>Search.</summary>
    Search,
    /// <summary>Enter (on-screen keyboard).</summary>
    Enter,
    /// <summary>Volume up (Roku TVs / audio-capable devices).</summary>
    VolumeUp,
    /// <summary>Volume down.</summary>
    VolumeDown,
    /// <summary>Mute toggle.</summary>
    VolumeMute,
    /// <summary>Power on.</summary>
    PowerOn,
    /// <summary>Power off.</summary>
    PowerOff,
    /// <summary>Power toggle.</summary>
    Power,
}
