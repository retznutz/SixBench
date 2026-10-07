using SixBench.Common.Utilities;
using SixBench.Services.Exceptions;

namespace SixBench.Api.Infrastructure;

/// <summary>
/// Helpers for decoding route keys.
/// </summary>
public static class RouteKeys
{
    /// <summary>
    /// Decodes a capture-device route key to its stable id.
    /// </summary>
    /// <param name="id">The base64url route key.</param>
    /// <returns>The stable id.</returns>
    /// <exception cref="ServiceValidationException">The key is malformed.</exception>
    public static string ToStableId(string id) =>
        DeviceKey.TryDecode(id, out var stableId)
            ? stableId
            : throw new ServiceValidationException($"'{id}' is not a valid capture device id.");
}
