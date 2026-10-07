using System.ComponentModel.DataAnnotations;
using SixBench.Common.Security;

namespace SixBench.Common.Dtos;

/// <summary>
/// Request body for an administrator to create a user. The user must change the password at first sign-in.
/// </summary>
public sealed class CreateUserRequest
{
    /// <summary>User name (letters, digits and <c>-._@+</c>).</summary>
    [Required, StringLength(256, MinimumLength = 2)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>Optional email address.</summary>
    [EmailAddress, StringLength(256)]
    public string? Email { get; set; }

    /// <summary>Initial password.</summary>
    [Required, StringLength(256)]
    public string Password { get; set; } = string.Empty;

    /// <summary>Role: <c>Admin</c> or <c>User</c>.</summary>
    [Required, AllowedValues(AppRoles.Admin, AppRoles.User)]
    public string Role { get; set; } = AppRoles.User;
}
