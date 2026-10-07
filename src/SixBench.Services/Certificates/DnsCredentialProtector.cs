using Microsoft.AspNetCore.DataProtection;

namespace SixBench.Services.Certificates;

/// <summary>
/// Encrypts DNS provider credentials for storage, so renewal can use them without keeping them in plain text.
/// </summary>
public interface IDnsCredentialProtector
{
    /// <summary>Encrypts each value.</summary>
    /// <param name="credentials">Credential key → value.</param>
    /// <returns>Credential key → encrypted value. Blank entries are dropped.</returns>
    Dictionary<string, string> Protect(IReadOnlyDictionary<string, string> credentials);

    /// <summary>Decrypts each value.</summary>
    /// <param name="encryptedCredentials">Credential key → encrypted value.</param>
    /// <returns>Credential key → value.</returns>
    Dictionary<string, string> Unprotect(IReadOnlyDictionary<string, string> encryptedCredentials);
}

/// <summary>
/// <see cref="IDnsCredentialProtector"/> using ASP.NET Core Data Protection (keys in <c>data/keys</c>).
/// </summary>
/// <param name="provider">Data protection provider.</param>
public sealed class DnsCredentialProtector(IDataProtectionProvider provider) : IDnsCredentialProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("SixBench.DnsCredentials.v1");

    /// <inheritdoc />
    public Dictionary<string, string> Protect(IReadOnlyDictionary<string, string> credentials) =>
        credentials
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
            .ToDictionary(pair => pair.Key, pair => _protector.Protect(pair.Value));

    /// <inheritdoc />
    public Dictionary<string, string> Unprotect(IReadOnlyDictionary<string, string> encryptedCredentials) =>
        encryptedCredentials
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
            .ToDictionary(pair => pair.Key, pair => _protector.Unprotect(pair.Value));
}
