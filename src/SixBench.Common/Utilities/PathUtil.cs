namespace SixBench.Common.Utilities;

/// <summary>
/// Path helpers that normalize to forward slashes so paths behave the same on Windows, macOS and Linux.
/// </summary>
public static class PathUtil
{
    /// <summary>
    /// Converts back slashes to forward slashes and collapses duplicate separators.
    /// A leading <c>//</c> (UNC path) is preserved.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized path, or an empty string for null/blank input.</returns>
    public static string Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var replaced = path.Trim().Replace('\\', '/');
        var isUnc = replaced.StartsWith("//", StringComparison.Ordinal);
        var body = isUnc ? replaced[2..] : replaced;

        while (body.Contains("//", StringComparison.Ordinal))
        {
            body = body.Replace("//", "/", StringComparison.Ordinal);
        }

        return isUnc ? "//" + body : body;
    }

    /// <summary>
    /// Joins path segments with forward slashes and normalizes the result.
    /// </summary>
    /// <param name="segments">Segments to join.</param>
    /// <returns>The combined, normalized path.</returns>
    public static string Combine(params string[] segments)
    {
        var parts = segments
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Replace('\\', '/'))
            .ToArray();
        if (parts.Length == 0)
        {
            return string.Empty;
        }

        return Normalize(Path.Combine(parts).Replace('\\', '/'));
    }

    /// <summary>
    /// Resolves a possibly relative path against <paramref name="basePath"/> and normalizes it.
    /// </summary>
    /// <param name="path">Absolute or relative path.</param>
    /// <param name="basePath">Directory that relative paths are resolved against.</param>
    /// <returns>An absolute, normalized path.</returns>
    public static string ResolveAgainst(string path, string basePath)
    {
        var normalized = Normalize(path);
        return Path.IsPathRooted(normalized)
            ? normalized
            : Normalize(Path.GetFullPath(Path.Combine(basePath, normalized)));
    }
}
