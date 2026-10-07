using System.ComponentModel.DataAnnotations;

namespace SixBench.Common.Dtos;

/// <summary>
/// Request body to sign in.
/// </summary>
public sealed class LoginRequest
{
    /// <summary>User name.</summary>
    [Required, StringLength(256)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>Password.</summary>
    [Required, StringLength(256)]
    public string Password { get; set; } = string.Empty;

    /// <summary>Keep the user signed in after the browser closes.</summary>
    public bool RememberMe { get; set; }
}
