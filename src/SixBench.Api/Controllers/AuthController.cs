using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SixBench.Common.Dtos;
using SixBench.Data.Entities;
using SixBench.Services.Users;

namespace SixBench.Api.Controllers;

/// <summary>
/// Sign-in, sign-out and the signed-in user's own account.
/// </summary>
/// <param name="signIn">Identity sign-in manager.</param>
/// <param name="users">User service.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController(SignInManager<AppUser> signIn, IUserService users) : ControllerBase
{
    /// <summary>
    /// Signs in with a user name and password and sets the sign-in cookie.
    /// Five failed attempts lock the account for five minutes.
    /// </summary>
    /// <param name="request">User name, password and whether to stay signed in.</param>
    /// <returns>The signed-in user.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<CurrentUserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserDto>> Login(LoginRequest request)
    {
        var result = await signIn.PasswordSignInAsync(request.UserName.Trim(), request.Password, request.RememberMe, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Account locked",
                detail: "Too many failed attempts. Try again in a few minutes.");
        }

        if (!result.Succeeded)
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Sign-in failed",
                detail: "Incorrect user name or password.");
        }

        var user = await signIn.UserManager.FindByNameAsync(request.UserName.Trim());
        await users.RecordLoginAsync(user!.Id);
        return Ok(await users.GetCurrentAsync(user.Id));
    }

    /// <summary>
    /// Signs out and clears the sign-in cookie.
    /// </summary>
    /// <returns>204 when signed out.</returns>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();
        return NoContent();
    }

    /// <summary>
    /// Gets the signed-in user.
    /// </summary>
    /// <returns>The user.</returns>
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserDto>> Me() => Ok(await users.GetCurrentAsync(CurrentUserId));

    /// <summary>
    /// Changes the signed-in user's password.
    /// </summary>
    /// <param name="request">Current and new password.</param>
    /// <returns>The updated user.</returns>
    [HttpPost("me/password")]
    [ProducesResponseType<CurrentUserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CurrentUserDto>> ChangePassword(ChangePasswordRequest request)
    {
        await users.ChangeOwnPasswordAsync(CurrentUserId, request);

        // The password change updates the security stamp; reissue the cookie so this session stays valid.
        var user = await signIn.UserManager.FindByIdAsync(CurrentUserId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await signIn.RefreshSignInAsync(user!);
        return Ok(await users.GetCurrentAsync(CurrentUserId));
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, System.Globalization.CultureInfo.InvariantCulture);
}
