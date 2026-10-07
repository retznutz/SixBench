namespace SixBench.Common.Dtos;

/// <summary>
/// A user account, as seen by an administrator.
/// </summary>
/// <param name="Id">User id.</param>
/// <param name="UserName">User name.</param>
/// <param name="Email">Email address, if set.</param>
/// <param name="Role">The user's role (<c>Admin</c> or <c>User</c>).</param>
/// <param name="MustChangePassword">True until the user replaces an assigned password.</param>
/// <param name="IsLockedOut">True while sign-in is locked after repeated failures.</param>
/// <param name="CreatedUtc">When the account was created.</param>
/// <param name="LastLoginUtc">When the user last signed in.</param>
public sealed record UserDto(
    int Id,
    string UserName,
    string? Email,
    string Role,
    bool MustChangePassword,
    bool IsLockedOut,
    DateTime CreatedUtc,
    DateTime? LastLoginUtc);
