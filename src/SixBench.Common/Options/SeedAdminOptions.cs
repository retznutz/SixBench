namespace SixBench.Common.Options;

/// <summary>
/// The administrator account created on first start (when there are no users), bound from <c>Identity:SeedAdmin</c>.
/// The account must change its password at first sign-in.
/// </summary>
public sealed class SeedAdminOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Identity:SeedAdmin";

    /// <summary>User name.</summary>
    public string UserName { get; set; } = "admin";

    /// <summary>Initial password. When empty, a random password is generated and written to the log.</summary>
    public string? Password { get; set; }
}
