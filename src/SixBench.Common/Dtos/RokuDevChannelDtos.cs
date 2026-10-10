using System.ComponentModel.DataAnnotations;

namespace SixBench.Common.Dtos;

/// <summary>
/// A message the Roku's developer web server showed for an action (the banners on its web page).
/// </summary>
/// <param name="Type"><c>success</c>, <c>info</c> or <c>error</c>.</param>
/// <param name="Text">The message, as the Roku worded it.</param>
public sealed record RokuDevMessageDto(string Type, string Text);

/// <summary>
/// Outcome of a developer web server action that succeeded.
/// </summary>
/// <param name="Summary">One-line summary for the user.</param>
/// <param name="Messages">Every message the Roku returned.</param>
public sealed record RokuDevActionResultDto(string Summary, IReadOnlyList<RokuDevMessageDto> Messages);

/// <summary>
/// A channel installed on the Roku, from ECP <c>/query/apps</c>.
/// </summary>
/// <param name="Id">Channel id (<c>dev</c> for the sideloaded channel).</param>
/// <param name="Name">Channel name.</param>
/// <param name="Version">Channel version, if reported.</param>
public sealed record RokuAppDto(string Id, string Name, string? Version);

/// <summary>
/// Developer-mode state of a Roku.
/// </summary>
/// <param name="DeveloperModeEnabled">Whether developer mode is on (from device-info); null if the Roku did not say.</param>
/// <param name="KeyedDeveloperId">Developer id of the signing key on the Roku; null if no key is set (packaging needs one).</param>
/// <param name="HasDevPassword">True if the developer-mode password is saved in SixBench.</param>
/// <param name="DevChannel">The sideloaded channel, if one is installed.</param>
public sealed record RokuDevChannelStatusDto(
    bool? DeveloperModeEnabled,
    string? KeyedDeveloperId,
    bool HasDevPassword,
    RokuAppDto? DevChannel);

/// <summary>
/// Request body to save a Roku's developer-mode password.
/// </summary>
public sealed class SetDevPasswordRequest
{
    /// <summary>The password set when developer mode was enabled (user <c>rokudev</c>).</summary>
    [Required, StringLength(256, MinimumLength = 1)]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Request body to package the sideloaded channel into a signed <c>.pkg</c>.
/// </summary>
public sealed class PackageChannelRequest
{
    /// <summary>Channel name for the package.</summary>
    [Required, StringLength(100, MinimumLength = 1)]
    public string AppName { get; set; } = string.Empty;

    /// <summary>Version, e.g. <c>1.0</c> or <c>1.2.3</c>.</summary>
    [Required, StringLength(32, MinimumLength = 1)]
    [RegularExpression(@"^\d+(\.\d+){0,3}$", ErrorMessage = "Version must look like 1.0 or 1.2.3.")]
    public string Version { get; set; } = string.Empty;

    /// <summary>Signing key password (from <c>genkey</c>). Used for this request only; never stored.</summary>
    [Required, StringLength(256, MinimumLength = 1)]
    public string SigningPassword { get; set; } = string.Empty;
}

/// <summary>
/// State of a Roku's BrightScript debug console connection.
/// </summary>
/// <param name="RokuId">Roku id.</param>
/// <param name="State"><c>Connecting</c>, <c>Connected</c> or <c>Disconnected</c>.</param>
/// <param name="Message">Why it disconnected, if it did.</param>
public sealed record RokuConsoleStatusDto(int RokuId, string State, string? Message);

/// <summary>
/// A piece of debug console output.
/// </summary>
/// <param name="RokuId">Roku id.</param>
/// <param name="Sequence">Increasing number; output at or below a snapshot's sequence is already in its backlog.</param>
/// <param name="Text">The text, as received (may end mid-line).</param>
public sealed record RokuConsoleOutputDto(int RokuId, long Sequence, string Text);

/// <summary>
/// What a viewer receives when it starts watching a debug console.
/// </summary>
/// <param name="Status">Connection state.</param>
/// <param name="Backlog">Recent output.</param>
/// <param name="Sequence">Sequence of the last output included in <paramref name="Backlog"/>.</param>
public sealed record RokuConsoleSnapshotDto(RokuConsoleStatusDto Status, string Backlog, long Sequence);
