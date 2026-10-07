using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixBench.Common.Options;
using SixBench.Common.Security;
using SixBench.Data.Entities;

namespace SixBench.Services.Users;

/// <summary>
/// Creates the roles and, on first start, the administrator account.
/// </summary>
public static class IdentitySeeder
{
    /// <summary>
    /// Ensures both roles exist and, when there are no users, creates the seed administrator from
    /// <see cref="SeedAdminOptions"/>. The administrator must change the password at first sign-in.
    /// </summary>
    /// <param name="services">A scoped service provider.</param>
    /// <returns>A task that completes when seeding is done.</returns>
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roles = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
        foreach (var role in AppRoles.All)
        {
            if (!await roles.RoleExistsAsync(role))
            {
                UserService.Check(await roles.CreateAsync(new IdentityRole<int>(role)), string.Empty);
            }
        }

        var users = services.GetRequiredService<UserManager<AppUser>>();
        if (await users.Users.AnyAsync())
        {
            return;
        }

        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(IdentitySeeder));
        var options = services.GetRequiredService<IOptions<SeedAdminOptions>>().Value;
        var password = options.Password;
        if (string.IsNullOrWhiteSpace(password))
        {
            password = "Sb-" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(12)).Replace('/', 'x').Replace('+', 'y') + "9a";
            logger.LogWarning("Created administrator {UserName} with generated password {Password}. Change it after signing in.", options.UserName, password);
        }
        else
        {
            logger.LogWarning("Created administrator {UserName} with the password from Identity:SeedAdmin. Change it after signing in.", options.UserName);
        }

        var admin = new AppUser
        {
            UserName = options.UserName,
            MustChangePassword = true,
            CreatedUtc = DateTime.UtcNow,
        };
        UserService.Check(await users.CreateAsync(admin, password), "password");
        UserService.Check(await users.AddToRoleAsync(admin, AppRoles.Admin), string.Empty);
    }
}
