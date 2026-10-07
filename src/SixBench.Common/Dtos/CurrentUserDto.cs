namespace SixBench.Common.Dtos;

/// <summary>
/// The signed-in user.
/// </summary>
/// <param name="Id">User id.</param>
/// <param name="UserName">User name.</param>
/// <param name="Email">Email address, if set.</param>
/// <param name="Role">The user's role (<c>Admin</c> or <c>User</c>).</param>
/// <param name="MustChangePassword">True until the user replaces a seeded or admin-assigned password.</param>
public sealed record CurrentUserDto(int Id, string UserName, string? Email, string Role, bool MustChangePassword);
