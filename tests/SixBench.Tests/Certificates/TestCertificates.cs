using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Certes;
using Certes.Acme;

namespace SixBench.Tests.Certificates;

/// <summary>Builds PFX files the same way production does: Certes key + chain → ToPfx → Build(name, "").</summary>
internal static class TestCertificates
{
    public static byte[] CreatePfx(string domain, DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        using var caKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var caRequest = new CertificateRequest("CN=Test Intermediate", caKey, HashAlgorithmName.SHA256);
        caRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        using var ca = caRequest.CreateSelfSigned(notBefore.AddDays(-1), notAfter.AddDays(1));

        var leafKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
        using var leafEcdsa = ECDsa.Create();
        leafEcdsa.ImportFromPem(leafKey.ToPem());
        var leafRequest = new CertificateRequest($"CN={domain}", leafEcdsa, HashAlgorithmName.SHA256);
        using var leaf = leafRequest.Create(ca, notBefore, notAfter, RandomNumberGenerator.GetBytes(8));

        var chainPem = leaf.ExportCertificatePem() + "\n" + ca.ExportCertificatePem() + "\n";
        return new CertificateChain(chainPem).ToPfx(leafKey).Build(domain, string.Empty);
    }

    public static string WritePfx(string directory, string domain, DateTimeOffset notBefore, DateTimeOffset notAfter, string fileName = "sixbench-cert.pfx")
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, fileName);
        File.WriteAllBytes(path, CreatePfx(domain, notBefore, notAfter));
        return path;
    }
}
