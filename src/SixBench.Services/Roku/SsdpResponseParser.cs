namespace SixBench.Services.Roku;

/// <summary>
/// Parses SSDP (HTTP-over-UDP) responses to a <c>roku:ecp</c> M-SEARCH.
/// </summary>
public static class SsdpResponseParser
{
    /// <summary>The M-SEARCH request for Roku devices.</summary>
    public const string RokuSearchRequest =
        "M-SEARCH * HTTP/1.1\r\n" +
        "Host: 239.255.255.250:1900\r\n" +
        "Man: \"ssdp:discover\"\r\n" +
        "ST: roku:ecp\r\n" +
        "MX: 2\r\n\r\n";

    /// <summary>
    /// Extracts the ECP base address from an SSDP response.
    /// </summary>
    /// <param name="response">The raw response text.</param>
    /// <returns>The location (e.g. <c>http://192.168.1.20:8060/</c>), or null if this is not a Roku ECP response.</returns>
    public static Uri? ParseLocation(string response)
    {
        if (string.IsNullOrWhiteSpace(response)
            || !response.StartsWith("HTTP/1.1 200", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string? location = null;
        var isRoku = false;
        foreach (var line in response.Split("\r\n", StringSplitOptions.RemoveEmptyEntries))
        {
            var colon = line.IndexOf(':');
            if (colon <= 0)
            {
                continue;
            }

            var key = line[..colon].Trim();
            var value = line[(colon + 1)..].Trim();
            if (key.Equals("LOCATION", StringComparison.OrdinalIgnoreCase))
            {
                location = value;
            }
            else if ((key.Equals("ST", StringComparison.OrdinalIgnoreCase) || key.Equals("USN", StringComparison.OrdinalIgnoreCase))
                && value.Contains("roku:ecp", StringComparison.OrdinalIgnoreCase))
            {
                isRoku = true;
            }
        }

        return isRoku && Uri.TryCreate(location, UriKind.Absolute, out var uri) ? uri : null;
    }
}
