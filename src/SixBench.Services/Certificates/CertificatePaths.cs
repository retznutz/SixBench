using SixBench.Common.Options;
using SixBench.Common.Utilities;

namespace SixBench.Services.Certificates;

/// <summary>
/// Where the certificate and ACME account key live.
/// </summary>
public static class CertificatePaths
{
    /// <summary>Certificate file name.</summary>
    public const string CertificateFileName = "sixbench-cert.pfx";

    /// <summary>ACME account key file name.</summary>
    public const string AccountKeyFileName = "acme-account.pem";

    /// <summary>Absolute path of the certificate (PFX, no password).</summary>
    /// <param name="tls">TLS options.</param>
    /// <param name="contentRoot">App folder.</param>
    /// <returns>The path.</returns>
    public static string Certificate(TlsOptions tls, string contentRoot) =>
        PathUtil.Combine(Directory(tls, contentRoot), CertificateFileName);

    /// <summary>Absolute path of the ACME account key (PEM).</summary>
    /// <param name="tls">TLS options.</param>
    /// <param name="contentRoot">App folder.</param>
    /// <returns>The path.</returns>
    public static string AccountKey(TlsOptions tls, string contentRoot) =>
        PathUtil.Combine(Directory(tls, contentRoot), AccountKeyFileName);

    private static string Directory(TlsOptions tls, string contentRoot) =>
        PathUtil.ResolveAgainst(tls.CertificateDirectory, contentRoot);
}
