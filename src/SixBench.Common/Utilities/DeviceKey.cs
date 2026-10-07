using System.Buffers.Text;
using System.Text;

namespace SixBench.Common.Utilities;

/// <summary>
/// Converts capture-device stable ids to and from URL-safe route keys.
/// Stable ids can contain characters that are awkward in URLs (DirectShow monikers contain
/// <c>\</c>, <c>?</c> and <c>#</c>), so routes use the base64url form instead.
/// </summary>
public static class DeviceKey
{
    /// <summary>
    /// Encodes a stable id as a base64url route key.
    /// </summary>
    /// <param name="stableId">The stable id.</param>
    /// <returns>The route key.</returns>
    public static string Encode(string stableId) => Base64Url.EncodeToString(Encoding.UTF8.GetBytes(stableId));

    /// <summary>
    /// Decodes a base64url route key.
    /// </summary>
    /// <param name="key">The route key.</param>
    /// <param name="stableId">The decoded stable id.</param>
    /// <returns>True when <paramref name="key"/> is valid base64url.</returns>
    public static bool TryDecode(string? key, out string stableId)
    {
        stableId = string.Empty;
        if (string.IsNullOrWhiteSpace(key) || !Base64Url.IsValid(key))
        {
            return false;
        }

        stableId = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(key));
        return stableId.Length > 0;
    }
}
