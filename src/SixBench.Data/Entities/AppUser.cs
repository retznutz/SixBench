using Microsoft.AspNetCore.Identity;

namespace SixBench.Data.Entities;

/// <summary>
/// A user account (ASP.NET Core Identity), stored in the <c>User</c> table.
/// </summary>
public class AppUser : IdentityUser<int>
{
    /// <summary>True until the user replaces a seeded or admin-assigned password.</summary>
    public bool MustChangePassword { get; set; }

    /// <summary>When the account was created.</summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>When the user last signed in.</summary>
    public DateTime? LastLoginUtc { get; set; }
}
