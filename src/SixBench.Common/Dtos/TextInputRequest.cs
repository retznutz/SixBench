using System.ComponentModel.DataAnnotations;

namespace SixBench.Common.Dtos;

/// <summary>
/// Request body to type text into the Roku's on-screen keyboard.
/// </summary>
public sealed class TextInputRequest
{
    /// <summary>The text to type, one character at a time.</summary>
    [Required, StringLength(256, MinimumLength = 1)]
    public string Text { get; set; } = string.Empty;
}
