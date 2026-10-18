namespace SixBench.Common.Dtos;

/// <summary>
/// Whether a Roku's on-screen keyboard is open, pushed over <c>/hubs/roku-remote</c> as <c>TextEditState</c>.
/// </summary>
/// <param name="RokuId">Roku id.</param>
/// <param name="Available">
/// SixBench is connected to the Roku's ECP-2 websocket and can tell. While false, <paramref name="Open"/> is false.
/// </param>
/// <param name="Open">A text field on the Roku is being edited (its keyboard is on screen).</param>
/// <param name="TextEditId">The Roku's id for that text field; a new id means a different field. Null when closed.</param>
/// <param name="Text">What the field contains, when the Roku reports it.</param>
public sealed record RokuTextEditStateDto(int RokuId, bool Available, bool Open, string? TextEditId, string? Text);
