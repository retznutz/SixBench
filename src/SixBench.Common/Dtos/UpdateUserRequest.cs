using System.ComponentModel.DataAnnotations;
using SixBench.Common.Security;

namespace SixBench.Common.Dtos;

/// <summary>
/// Request body for an administrator to change a user's email or role.
/// </summary>
public sealed class UpdateUserRequest
{
    /// <summary>Email address, or null to clear it.</summary>
    [EmailAddress, StringLength(256)]
    public string? Email { get; set; }

    /// <summary>Role: <c>Admin</c> or <c>User</c>.</summary>
    [Required, AllowedValues(AppRoles.Admin, AppRoles.User)]
    public string Role { get; set; } = AppRoles.User;
}
