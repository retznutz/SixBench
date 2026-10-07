using System.ComponentModel.DataAnnotations;

namespace SixBench.Common.Dtos;

/// <summary>
/// Request body for a user to change their own password.
/// </summary>
public sealed class ChangePasswordRequest
{
    /// <summary>The current password.</summary>
    [Required, StringLength(256)]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>The new password.</summary>
    [Required, StringLength(256)]
    public string NewPassword { get; set; } = string.Empty;
}
