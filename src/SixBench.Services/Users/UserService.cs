using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SixBench.Common.Dtos;
using SixBench.Common.Security;
using SixBench.Data.Entities;
using SixBench.Services.Exceptions;

namespace SixBench.Services.Users;

/// <summary>
/// User accounts and roles.
/// </summary>
public interface IUserService
{
    /// <summary>Lists all users by name.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The users.</returns>
    Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken ct = default);

    /// <summary>Gets a user.</summary>
    /// <param name="id">User id.</param>
    /// <returns>The user.</returns>
    /// <exception cref="NotFoundException">No such user.</exception>
    Task<UserDto> GetAsync(int id);

    /// <summary>Gets the signed-in user's details.</summary>
    /// <param name="id">User id.</param>
    /// <returns>The user.</returns>
    /// <exception cref="NotFoundException">No such user.</exception>
    Task<CurrentUserDto> GetCurrentAsync(int id);

    /// <summary>Creates a user who must change the password at first sign-in.</summary>
    /// <param name="request">Name, email, password and role.</param>
    /// <returns>The new user.</returns>
    /// <exception cref="FieldValidationException">The name is taken or the password is too weak.</exception>
    Task<UserDto> CreateAsync(CreateUserRequest request);

    /// <summary>Changes a user's email and role. The last administrator cannot be demoted.</summary>
    /// <param name="id">User id.</param>
    /// <param name="request">Email and role.</param>
    /// <returns>The updated user.</returns>
    Task<UserDto> UpdateAsync(int id, UpdateUserRequest request);

    /// <summary>Sets a user's password; they must change it at next sign-in. Also clears any lockout.</summary>
    /// <param name="id">User id.</param>
    /// <param name="newPassword">The new password.</param>
    /// <returns>The updated user.</returns>
    Task<UserDto> ResetPasswordAsync(int id, string newPassword);

    /// <summary>Deletes a user. Administrators cannot delete themselves or the last administrator.</summary>
    /// <param name="id">User id.</param>
    /// <param name="currentUserId">The administrator making the request.</param>
    /// <returns>A task that completes when the user is deleted.</returns>
    Task DeleteAsync(int id, int currentUserId);

    /// <summary>Changes the signed-in user's own password and clears <see cref="AppUser.MustChangePassword"/>.</summary>
    /// <param name="id">User id.</param>
    /// <param name="request">Current and new password.</param>
    /// <returns>A task that completes when the password is changed.</returns>
    Task ChangeOwnPasswordAsync(int id, ChangePasswordRequest request);

    /// <summary>Records a successful sign-in.</summary>
    /// <param name="id">User id.</param>
    /// <returns>A task that completes when saved.</returns>
    Task RecordLoginAsync(int id);
}

/// <summary>
/// <see cref="IUserService"/> built on ASP.NET Core Identity's <see cref="UserManager{TUser}"/>.
/// </summary>
/// <param name="users">Identity user manager.</param>
/// <param name="time">Clock.</param>
public sealed class UserService(UserManager<AppUser> users, TimeProvider time) : IUserService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken ct = default)
    {
        var admins = await AdminIdsAsync();
        var all = await users.Users.AsNoTracking().OrderBy(u => u.UserName).ToListAsync(ct);
        return all.Select(u => ToDto(u, admins.Contains(u.Id))).ToList();
    }

    /// <inheritdoc />
    public async Task<UserDto> GetAsync(int id)
    {
        var user = await FindAsync(id);
        return ToDto(user, await users.IsInRoleAsync(user, AppRoles.Admin));
    }

    /// <inheritdoc />
    public async Task<CurrentUserDto> GetCurrentAsync(int id)
    {
        var user = await FindAsync(id);
        var role = await users.IsInRoleAsync(user, AppRoles.Admin) ? AppRoles.Admin : AppRoles.User;
        return new CurrentUserDto(user.Id, user.UserName!, user.Email, role, user.MustChangePassword);
    }

    /// <inheritdoc />
    public async Task<UserDto> CreateAsync(CreateUserRequest request)
    {
        var user = new AppUser
        {
            UserName = request.UserName.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            MustChangePassword = true,
            CreatedUtc = time.GetUtcNow().UtcDateTime,
        };
        Check(await users.CreateAsync(user, request.Password), nameof(CreateUserRequest.Password));
        Check(await users.AddToRoleAsync(user, request.Role), nameof(CreateUserRequest.Role));
        return ToDto(user, request.Role == AppRoles.Admin);
    }

    /// <inheritdoc />
    public async Task<UserDto> UpdateAsync(int id, UpdateUserRequest request)
    {
        var user = await FindAsync(id);
        var isAdmin = await users.IsInRoleAsync(user, AppRoles.Admin);
        var makeAdmin = request.Role == AppRoles.Admin;
        if (isAdmin && !makeAdmin)
        {
            await EnsureAnotherAdminAsync(user.Id, "demote");
        }

        user.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        Check(await users.UpdateAsync(user), nameof(UpdateUserRequest.Email));

        if (isAdmin != makeAdmin)
        {
            Check(await users.RemoveFromRolesAsync(user, AppRoles.All.Where(r => r != request.Role)), nameof(UpdateUserRequest.Role));
            Check(await users.AddToRoleAsync(user, request.Role), nameof(UpdateUserRequest.Role));

            // Existing sessions pick up the new role at their next security-stamp check.
            await users.UpdateSecurityStampAsync(user);
        }

        return ToDto(user, makeAdmin);
    }

    /// <inheritdoc />
    public async Task<UserDto> ResetPasswordAsync(int id, string newPassword)
    {
        var user = await FindAsync(id);
        var token = await users.GeneratePasswordResetTokenAsync(user);
        Check(await users.ResetPasswordAsync(user, token, newPassword), nameof(ResetPasswordRequest.NewPassword));

        user.MustChangePassword = true;
        user.LockoutEnd = null;
        user.AccessFailedCount = 0;
        Check(await users.UpdateAsync(user), string.Empty);
        return ToDto(user, await users.IsInRoleAsync(user, AppRoles.Admin));
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, int currentUserId)
    {
        if (id == currentUserId)
        {
            throw new ServiceValidationException("You can't delete your own account.");
        }

        var user = await FindAsync(id);
        if (await users.IsInRoleAsync(user, AppRoles.Admin))
        {
            await EnsureAnotherAdminAsync(user.Id, "delete");
        }

        Check(await users.DeleteAsync(user), string.Empty);
    }

    /// <inheritdoc />
    public async Task ChangeOwnPasswordAsync(int id, ChangePasswordRequest request)
    {
        var user = await FindAsync(id);
        if (!await users.CheckPasswordAsync(user, request.CurrentPassword))
        {
            throw new FieldValidationException(new Dictionary<string, string[]>
            {
                [nameof(ChangePasswordRequest.CurrentPassword)] = ["The current password is incorrect."],
            });
        }

        if (request.CurrentPassword == request.NewPassword)
        {
            throw new FieldValidationException(new Dictionary<string, string[]>
            {
                [nameof(ChangePasswordRequest.NewPassword)] = ["Choose a password different from the current one."],
            });
        }

        Check(await users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword), nameof(ChangePasswordRequest.NewPassword));
        user.MustChangePassword = false;
        Check(await users.UpdateAsync(user), string.Empty);
    }

    /// <inheritdoc />
    public async Task RecordLoginAsync(int id)
    {
        var user = await FindAsync(id);
        user.LastLoginUtc = time.GetUtcNow().UtcDateTime;
        await users.UpdateAsync(user);
    }

    /// <summary>
    /// Throws a <see cref="FieldValidationException"/> for a failed Identity result, keying each error by the field it concerns.
    /// </summary>
    /// <param name="result">The Identity result.</param>
    /// <param name="passwordField">The request field that holds the password (password errors are reported against it).</param>
    public static void Check(IdentityResult result, string passwordField)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = result.Errors
            .GroupBy(e => e.Code switch
            {
                _ when e.Code.StartsWith("Password", StringComparison.Ordinal) => passwordField,
                _ when e.Code.Contains("UserName", StringComparison.Ordinal) => nameof(CreateUserRequest.UserName),
                _ when e.Code.Contains("Email", StringComparison.Ordinal) => nameof(CreateUserRequest.Email),
                _ when e.Code.Contains("Role", StringComparison.Ordinal) => nameof(CreateUserRequest.Role),
                _ => string.Empty,
            })
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
        throw new FieldValidationException(errors);
    }

    private async Task<AppUser> FindAsync(int id) =>
        await users.FindByIdAsync(id.ToString(System.Globalization.CultureInfo.InvariantCulture))
        ?? throw new NotFoundException($"User {id} was not found.");

    private async Task<HashSet<int>> AdminIdsAsync() =>
        (await users.GetUsersInRoleAsync(AppRoles.Admin)).Select(u => u.Id).ToHashSet();

    private async Task EnsureAnotherAdminAsync(int userId, string action)
    {
        var admins = await AdminIdsAsync();
        if (admins.All(id => id == userId))
        {
            throw new ServiceValidationException($"You can't {action} the last administrator. Make another user an administrator first.");
        }
    }

    private UserDto ToDto(AppUser user, bool isAdmin) => new(
        user.Id,
        user.UserName!,
        user.Email,
        isAdmin ? AppRoles.Admin : AppRoles.User,
        user.MustChangePassword,
        user.LockoutEnd is { } end && end > time.GetUtcNow(),
        DateTime.SpecifyKind(user.CreatedUtc, DateTimeKind.Utc),
        user.LastLoginUtc is { } last ? DateTime.SpecifyKind(last, DateTimeKind.Utc) : null);
}
