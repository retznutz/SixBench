namespace SixBench.Common.Options;

/// <summary>
/// Encoder thumbnail settings, bound from the <c>Thumbnails</c> configuration section.
/// </summary>
public sealed class ThumbnailOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Thumbnails";

    /// <summary>Where encoder thumbnails are stored. Relative paths resolve against the app folder.</summary>
    public string Directory { get; set; } = "data/thumbnails";

    /// <summary>Largest thumbnail accepted from the web app, in kilobytes.</summary>
    public int MaxKilobytes { get; set; } = 1024;
}
