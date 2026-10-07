using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using SixBench.Common.Utilities;
using SixBench.Data;
using SixBench.Data.Entities;

namespace SixBench.Api.Infrastructure;

/// <summary>
/// ASP.NET Core Identity with cookie sign-in, set up for a single-page app.
/// </summary>
public static class AuthenticationSetup
{
    /// <summary>Name of the sign-in cookie.</summary>
    public const string CookieName = "SixBench.Auth";

    /// <summary>
    /// Adds Identity (users and roles in the SixBench database), the sign-in cookie, persistent
    /// data-protection keys, and a fallback policy that requires a signed-in user on every endpoint.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">App configuration (<c>DataProtection:KeysPath</c>).</param>
    /// <param name="contentRoot">Base directory for relative paths.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddSixBenchAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRoot)
    {
        services
            .AddIdentity<AppUser, IdentityRole<int>>(o =>
            {
                o.User.RequireUniqueEmail = false;
                o.Password.RequiredLength = 8;
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddEntityFrameworkStores<SixBenchDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(o =>
        {
            o.Cookie.Name = CookieName;
            o.Cookie.HttpOnly = true;
            o.Cookie.SameSite = SameSiteMode.Lax;

            // Until HTTPS is set up, LAN users sign in over plain HTTP.
            o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            o.ExpireTimeSpan = TimeSpan.FromDays(14);
            o.SlidingExpiration = true;

            // An API answers with status codes; the SPA shows its own login page.
            o.Events.OnRedirectToLogin = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            o.Events.OnRedirectToAccessDenied = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        // Role changes, password resets and deleted accounts reach existing sessions within a minute.
        services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(1));

        // Keep the cookie keys with the app so sign-ins survive restarts and moving the folder.
        var keysPath = PathUtil.ResolveAgainst(configuration["DataProtection:KeysPath"] ?? "data/keys", contentRoot);
        var dataProtection = services.AddDataProtection()
            .SetApplicationName("SixBench")
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        if (OperatingSystem.IsWindows())
        {
            dataProtection.ProtectKeysWithDpapi();
        }

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }
}
