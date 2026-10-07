namespace SixBench.Services.Certificates.Providers;

/// <summary>
/// Domain-name helpers shared by the DNS providers.
/// </summary>
public static class DnsNames
{
    /// <summary>
    /// The registered domain (zone): the last two labels, e.g. <c>example.com</c> for <c>sixbench.example.com</c>.
    /// Multi-label public suffixes such as <c>co.uk</c> are not recognised.
    /// </summary>
    /// <param name="domain">A domain name.</param>
    /// <returns>The root domain.</returns>
    public static string GetRootDomain(string domain)
    {
        var parts = domain.TrimEnd('.').Split('.');
        return parts.Length <= 2 ? domain : string.Join('.', parts[^2..]);
    }

    /// <summary>
    /// A record name relative to its zone, e.g. <c>_acme-challenge.sixbench</c> for
    /// <c>_acme-challenge.sixbench.example.com</c> in <c>example.com</c>.
    /// </summary>
    /// <param name="recordName">Fully qualified record name.</param>
    /// <param name="rootDomain">The zone.</param>
    /// <returns>The relative name, or <paramref name="recordName"/> if it isn't inside the zone.</returns>
    public static string GetRelativeRecordName(string recordName, string rootDomain) =>
        recordName.EndsWith($".{rootDomain}", StringComparison.OrdinalIgnoreCase)
            ? recordName[..^(rootDomain.Length + 1)]
            : recordName;
}
