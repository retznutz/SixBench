using SixBench.Common.Dtos;

namespace SixBench.Services.Links;

/// <summary>
/// Manages encoder-to-Roku links.
/// </summary>
public interface IEncoderLinkService
{
    /// <summary>Lists all links.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All links.</returns>
    Task<IReadOnlyList<EncoderLinkDto>> ListAsync(CancellationToken ct = default);

    /// <summary>Gets a link by capture-device stable id.</summary>
    /// <param name="stableId">Capture device stable id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The link.</returns>
    /// <exception cref="Exceptions.NotFoundException">No link exists.</exception>
    Task<EncoderLinkDto> GetAsync(string stableId, CancellationToken ct = default);

    /// <summary>
    /// Creates or updates a link. Any running stream for the encoder is restarted to apply new settings.
    /// </summary>
    /// <param name="stableId">Capture device stable id.</param>
    /// <param name="request">Link values.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The saved link.</returns>
    Task<EncoderLinkDto> UpsertAsync(string stableId, UpsertEncoderLinkRequest request, CancellationToken ct = default);

    /// <summary>Deletes a link.</summary>
    /// <param name="stableId">Capture device stable id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if deleted.</returns>
    Task<bool> DeleteAsync(string stableId, CancellationToken ct = default);
}
