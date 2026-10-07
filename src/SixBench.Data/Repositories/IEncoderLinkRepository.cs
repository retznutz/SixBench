using SixBench.Data.Entities;

namespace SixBench.Data.Repositories;

/// <summary>
/// Persistence for <see cref="EncoderLink"/>.
/// </summary>
public interface IEncoderLinkRepository
{
    /// <summary>Lists all links, including their Roku.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All links.</returns>
    Task<IReadOnlyList<EncoderLink>> ListAsync(CancellationToken ct = default);

    /// <summary>Gets a link by capture-device stable id, including its Roku.</summary>
    /// <param name="stableId">Capture device stable id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The link, or null.</returns>
    Task<EncoderLink?> GetByStableIdAsync(string stableId, CancellationToken ct = default);

    /// <summary>Saves a new or modified link and stamps timestamps.</summary>
    /// <param name="link">The link (tracked or new).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The saved link.</returns>
    Task<EncoderLink> SaveAsync(EncoderLink link, CancellationToken ct = default);

    /// <summary>Deletes a link by capture-device stable id.</summary>
    /// <param name="stableId">Capture device stable id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if a link was deleted.</returns>
    Task<bool> DeleteAsync(string stableId, CancellationToken ct = default);
}
