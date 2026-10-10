using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SixBench.Common.Options;
using SixBench.Common.Utilities;
using SixBench.Services.Exceptions;

namespace SixBench.Services.Capture;

/// <summary>
/// A saved encoder thumbnail.
/// </summary>
/// <param name="Path">Absolute path of the JPEG file.</param>
/// <param name="UpdatedUtc">When it was saved.</param>
public sealed record EncoderThumbnail(string Path, DateTime UpdatedUtc);

/// <summary>
/// Preview images of encoders, captured by the web app from their live video.
/// </summary>
public interface IEncoderThumbnailStore
{
    /// <summary>
    /// Finds an encoder's thumbnail.
    /// </summary>
    /// <param name="stableId">Capture device stable id.</param>
    /// <returns>The thumbnail, or null if none has been saved.</returns>
    EncoderThumbnail? Find(string stableId);

    /// <summary>
    /// Saves an encoder's thumbnail, replacing any existing one.
    /// </summary>
    /// <param name="stableId">Capture device stable id.</param>
    /// <param name="jpeg">The image.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="ServiceValidationException">The image is empty, too large or not a JPEG.</exception>
    Task SaveAsync(string stableId, byte[] jpeg, CancellationToken ct = default);
}

/// <summary>
/// <see cref="IEncoderThumbnailStore"/> that keeps one JPEG per encoder in <see cref="ThumbnailOptions.Directory"/>.
/// </summary>
/// <param name="options">Thumbnail options.</param>
/// <param name="environment">Host environment (relative directories resolve against its content root).</param>
public sealed class EncoderThumbnailStore(IOptions<ThumbnailOptions> options, IHostEnvironment environment) : IEncoderThumbnailStore
{
    private string Root => PathUtil.ResolveAgainst(options.Value.Directory, environment.ContentRootPath);

    /// <inheritdoc />
    public EncoderThumbnail? Find(string stableId)
    {
        var file = new FileInfo(PathFor(stableId));
        return file.Exists ? new EncoderThumbnail(PathUtil.Normalize(file.FullName), file.LastWriteTimeUtc) : null;
    }

    /// <inheritdoc />
    public async Task SaveAsync(string stableId, byte[] jpeg, CancellationToken ct = default)
    {
        var maxKilobytes = options.Value.MaxKilobytes;
        if (jpeg.Length == 0)
        {
            throw new ServiceValidationException("The thumbnail is empty.");
        }

        if (jpeg.Length > maxKilobytes * 1024L)
        {
            throw new ServiceValidationException(
                $"The thumbnail is {jpeg.Length / 1024} KB; the limit is {maxKilobytes} KB (Thumbnails:MaxKilobytes).");
        }

        if (!IsJpeg(jpeg))
        {
            throw new ServiceValidationException("The thumbnail must be a JPEG image.");
        }

        Directory.CreateDirectory(Root);
        var path = PathFor(stableId);

        // Write beside the target and swap it in, so a request for the image never sees half a file.
        var temp = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            await File.WriteAllBytesAsync(temp, jpeg, ct);
            File.Move(temp, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temp))
            {
                File.Delete(temp);
            }
        }
    }

    /// <summary>
    /// File name for an encoder's thumbnail. Stable ids can be long and contain characters file systems reject
    /// (DirectShow monikers include <c>\</c>, <c>?</c> and <c>#</c>), so the name is a hash of the id.
    /// </summary>
    /// <param name="stableId">Capture device stable id.</param>
    /// <returns>The file name.</returns>
    public static string FileName(string stableId) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(stableId))) + ".jpg";

    private string PathFor(string stableId) => PathUtil.Combine(Root, FileName(stableId));

    // JPEG files start with an SOI marker followed by another marker.
    private static bool IsJpeg(byte[] data) => data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF;
}
