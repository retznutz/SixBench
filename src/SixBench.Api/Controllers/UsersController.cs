using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixBench.Common.Dtos;
using SixBench.Common.Security;
using SixBench.Services.Users;

namespace SixBench.Api.Controllers;

/// <summary>
/// User management (administrators only).
/// </summary>
/// <param name="users">User service.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Produces("application/json")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class UsersController(IUserService users) : ControllerBase
{
    /// <summary>
    /// Lists all users.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The users.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UserDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> List(CancellationToken ct) => Ok(await users.ListAsync(ct));

    /// <summary>
    /// Gets a user.
    /// </summary>
    /// <param name="id">User id.</param>
    /// <returns>The user.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> Get(int id) => Ok(await users.GetAsync(id));

    /// <summary>
    /// Creates a user. They must change the password at first sign-in.
    /// </summary>
    /// <param name="request">Name, optional email, initial password and role.</param>
    /// <returns>The new user.</returns>
    [HttpPost]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request)
    {
        var user = await users.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = user.Id, version = "1.0" }, user);
    }

    /// <summary>
    /// Changes a user's email and role. The last administrator cannot be demoted.
    /// </summary>
    /// <param name="id">User id.</param>
    /// <param name="request">Email and role.</param>
    /// <returns>The updated user.</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> Update(int id, UpdateUserRequest request) => Ok(await users.UpdateAsync(id, request));

    /// <summary>
    /// Sets a user's password and unlocks the account. They must change it at next sign-in.
    /// </summary>
    /// <param name="id">User id.</param>
    /// <param name="request">The new password.</param>
    /// <returns>The updated user.</returns>
    [HttpPost("{id:int}/password")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> ResetPassword(int id, ResetPasswordRequest request) =>
        Ok(await users.ResetPasswordAsync(id, request.NewPassword));

    /// <summary>
    /// Deletes a user. You can't delete yourself or the last administrator.
    /// </summary>
    /// <param name="id">User id.</param>
    /// <returns>204 when deleted.</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await users.DeleteAsync(id, int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, System.Globalization.CultureInfo.InvariantCulture));
        return NoContent();
    }
}
