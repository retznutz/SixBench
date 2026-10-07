using System.ComponentModel.DataAnnotations;

namespace SixBench.Common.Dtos;

/// <summary>
/// Request body for an administrator to set a user's password. The user must change it at next sign-in.
/// </summary>
public sealed class ResetPasswordRequest
{
    /// <summary>The new password.</summary>
    [Required, StringLength(256)]
    public string NewPassword { get; set; } = string.Empty;
}
