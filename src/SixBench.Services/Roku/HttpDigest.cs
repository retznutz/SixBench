using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace SixBench.Services.Roku;

/// <summary>
/// HTTP Digest authentication (RFC 7616 with MD5, as the Roku developer web server uses).
/// Done by hand rather than with handler credentials because each Roku has its own password
/// and the typed <see cref="HttpClient"/> shares one pooled handler.
/// </summary>
public static class HttpDigest
{
    /// <summary>
    /// Finds the Digest challenge among <c>WWW-Authenticate</c> headers.
    /// </summary>
    /// <param name="headers">The response's <c>WWW-Authenticate</c> values.</param>
    /// <returns>The challenge parameters (keys are case-insensitive), or null if there is no Digest challenge.</returns>
    public static IReadOnlyDictionary<string, string>? FindChallenge(HttpHeaderValueCollection<AuthenticationHeaderValue> headers)
    {
        var digest = headers.FirstOrDefault(h => string.Equals(h.Scheme, "Digest", StringComparison.OrdinalIgnoreCase));
        return digest?.Parameter is { Length: > 0 } parameter ? ParseParameters(parameter) : null;
    }

    /// <summary>
    /// Parses <c>key=value, key="quoted, value"</c> pairs.
    /// </summary>
    /// <param name="parameter">The challenge without the scheme.</param>
    /// <returns>Parameters; keys are case-insensitive.</returns>
    public static IReadOnlyDictionary<string, string> ParseParameters(string parameter)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var i = 0;
        while (i < parameter.Length)
        {
            while (i < parameter.Length && (parameter[i] == ',' || char.IsWhiteSpace(parameter[i])))
            {
                i++;
            }

            var keyStart = i;
            while (i < parameter.Length && parameter[i] != '=' && parameter[i] != ',')
            {
                i++;
            }

            var key = parameter[keyStart..i].Trim();
            if (i >= parameter.Length || parameter[i] != '=')
            {
                continue;
            }

            i++;
            string value;
            if (i < parameter.Length && parameter[i] == '"')
            {
                var sb = new StringBuilder();
                i++;
                while (i < parameter.Length && parameter[i] != '"')
                {
                    if (parameter[i] == '\\' && i + 1 < parameter.Length)
                    {
                        i++;
                    }

                    sb.Append(parameter[i]);
                    i++;
                }

                i++;
                value = sb.ToString();
            }
            else
            {
                var valueStart = i;
                while (i < parameter.Length && parameter[i] != ',')
                {
                    i++;
                }

                value = parameter[valueStart..i].Trim();
            }

            if (key.Length > 0)
            {
                result[key] = value;
            }
        }

        return result;
    }

    /// <summary>
    /// Builds the <c>Authorization: Digest ...</c> parameter for one request.
    /// </summary>
    /// <param name="challenge">Parameters from the server's challenge.</param>
    /// <param name="method">Request method.</param>
    /// <param name="uri">Request target (path and query).</param>
    /// <param name="userName">User name.</param>
    /// <param name="password">Password.</param>
    /// <param name="clientNonce">Client nonce (random hex; fixed only in tests).</param>
    /// <param name="nonceCount">Times this nonce has been used, starting at 1.</param>
    /// <returns>The header parameter (without the <c>Digest</c> scheme).</returns>
    /// <exception cref="NotSupportedException">The challenge asks for an algorithm other than MD5.</exception>
    public static string CreateAuthorization(
        IReadOnlyDictionary<string, string> challenge,
        string method,
        string uri,
        string userName,
        string password,
        string clientNonce,
        int nonceCount = 1)
    {
        challenge.TryGetValue("realm", out var realm);
        challenge.TryGetValue("nonce", out var nonce);
        challenge.TryGetValue("opaque", out var opaque);
        challenge.TryGetValue("algorithm", out var algorithm);
        realm ??= string.Empty;
        nonce ??= string.Empty;

        var sessionAlgorithm = string.Equals(algorithm, "MD5-sess", StringComparison.OrdinalIgnoreCase);
        if (algorithm is { Length: > 0 } && !sessionAlgorithm && !string.Equals(algorithm, "MD5", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"Digest algorithm '{algorithm}' is not supported.");
        }

        var qopOffered = challenge.TryGetValue("qop", out var qopList)
            && qopList.Split(',', StringSplitOptions.TrimEntries).Contains("auth", StringComparer.OrdinalIgnoreCase);
        var nc = nonceCount.ToString("x8", CultureInfo.InvariantCulture);

        var ha1 = Md5Hex($"{userName}:{realm}:{password}");
        if (sessionAlgorithm)
        {
            ha1 = Md5Hex($"{ha1}:{nonce}:{clientNonce}");
        }

        var ha2 = Md5Hex($"{method.ToUpperInvariant()}:{uri}");
        var response = qopOffered
            ? Md5Hex($"{ha1}:{nonce}:{nc}:{clientNonce}:auth:{ha2}")
            : Md5Hex($"{ha1}:{nonce}:{ha2}");

        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"username=\"{Escape(userName)}\", realm=\"{Escape(realm)}\", nonce=\"{Escape(nonce)}\", uri=\"{Escape(uri)}\"");
        if (algorithm is { Length: > 0 })
        {
            sb.Append(CultureInfo.InvariantCulture, $", algorithm={algorithm}");
        }

        if (qopOffered)
        {
            sb.Append(CultureInfo.InvariantCulture, $", qop=auth, nc={nc}, cnonce=\"{clientNonce}\"");
        }

        sb.Append(CultureInfo.InvariantCulture, $", response=\"{response}\"");
        if (opaque is not null)
        {
            sb.Append(CultureInfo.InvariantCulture, $", opaque=\"{Escape(opaque)}\"");
        }

        return sb.ToString();
    }

    /// <summary>Creates a random client nonce.</summary>
    /// <returns>16 random bytes as lowercase hex.</returns>
    public static string NewClientNonce() => Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));

    // Digest auth is defined over MD5; this is protocol compatibility, not a security choice.
#pragma warning disable CA5351
    private static string Md5Hex(string value) => Convert.ToHexStringLower(MD5.HashData(Encoding.UTF8.GetBytes(value)));
#pragma warning restore CA5351

    private static string Escape(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
}
