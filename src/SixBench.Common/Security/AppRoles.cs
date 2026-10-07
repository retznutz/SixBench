namespace SixBench.Common.Security;

/// <summary>
/// The application's roles. Every user has exactly one.
/// </summary>
public static class AppRoles
{
    /// <summary>Full access, including user management and HTTPS setup.</summary>
    public const string Admin = "Admin";

    /// <summary>Can watch and control Rokus and manage devices.</summary>
    public const string User = "User";

    /// <summary>All roles.</summary>
    public static readonly IReadOnlyList<string> All = [Admin, User];
}
