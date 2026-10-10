using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace SixBench.Services.Roku;

/// <summary>
/// Encrypts Roku developer-mode passwords for storage.
/// </summary>
public interface IRokuDevPasswordProtector
{
    /// <summary>Encrypts a password.</summary>
    /// <param name="password">The password.</param>
    /// <returns>The encrypted form.</returns>
    string Protect(string password);

    /// <summary>Decrypts a password.</summary>
    /// <param name="protectedPassword">The encrypted form.</param>
    /// <returns>The password, or null if it can no longer be decrypted (data-protection keys lost).</returns>
    string? Unprotect(string protectedPassword);
}

/// <summary>
/// <see cref="IRokuDevPasswordProtector"/> using ASP.NET Core Data Protection (keys in <c>data/keys</c>).
/// </summary>
/// <param name="provider">Data protection provider.</param>
public sealed class RokuDevPasswordProtector(IDataProtectionProvider provider) : IRokuDevPasswordProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("SixBench.RokuDevPassword.v1");

    /// <inheritdoc />
    public string Protect(string password) => _protector.Protect(password);

    /// <inheritdoc />
    public string? Unprotect(string protectedPassword)
    {
        try
        {
            return _protector.Unprotect(protectedPassword);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}
