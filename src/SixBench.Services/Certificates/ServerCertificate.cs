using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace SixBench.Services.Certificates;

/// <summary>
/// The certificate Kestrel serves. Kestrel asks for it on every TLS handshake, so loading a new file
/// (after a renewal) takes effect immediately.
/// </summary>
public interface IServerCertificate
{
    /// <summary>The current certificate, or null if none is loaded.</summary>
    X509Certificate2? Current { get; }

    /// <summary>Loads (or reloads) the certificate from a PFX file. Failures are logged and leave the current certificate in place.</summary>
    /// <param name="pfxPath">PFX path.</param>
    /// <returns>True if the certificate was loaded.</returns>
    bool Load(string pfxPath);
}

/// <summary>
/// Default <see cref="IServerCertificate"/>. Created before the host is built (Kestrel needs it) and then registered as a singleton.
/// </summary>
/// <param name="logger">Logger (startup uses the bootstrap logger).</param>
public sealed class ServerCertificate(ILogger logger) : IServerCertificate
{
    private volatile X509Certificate2? _current;

    /// <inheritdoc />
    public X509Certificate2? Current => _current;

    /// <inheritdoc />
    public bool Load(string pfxPath)
    {
        try
        {
            var cert = X509CertificateLoader.LoadPkcs12FromFile(pfxPath, null);
            var now = DateTime.Now;
            if (cert.NotAfter < now)
            {
                logger.LogError("Certificate has EXPIRED. Subject: {Subject}, expired {Expiry}", cert.Subject, cert.NotAfter);
            }
            else if (cert.NotAfter < now.AddDays(30))
            {
                logger.LogWarning("Certificate expires soon. Subject: {Subject}, expires {Expiry}", cert.Subject, cert.NotAfter);
            }

            if (cert.NotBefore > now)
            {
                logger.LogError("Certificate is not yet valid. Subject: {Subject}, valid from {NotBefore}", cert.Subject, cert.NotBefore);
            }

            if (!cert.HasPrivateKey)
            {
                logger.LogError("Certificate has no private key, so TLS will fail. Path: {Path}", pfxPath);
                return false;
            }

            logger.LogInformation(
                "Certificate loaded: {Subject}, issuer {Issuer}, valid {NotBefore} to {NotAfter}",
                cert.Subject, cert.Issuer, cert.NotBefore, cert.NotAfter);

            // The previous certificate is not disposed: a handshake in progress may still be using it.
            _current = cert;
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load certificate from {Path}", pfxPath);
            return false;
        }
    }
}
