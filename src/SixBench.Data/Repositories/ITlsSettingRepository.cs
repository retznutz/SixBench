using SixBench.Data.Entities;

namespace SixBench.Data.Repositories;

/// <summary>
/// Persistence for the single <see cref="TlsSetting"/> row.
/// </summary>
public interface ITlsSettingRepository
{
    /// <summary>Gets the row.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The row, or null if HTTPS has never been set up from the web app.</returns>
    Task<TlsSetting?> GetAsync(CancellationToken ct = default);

    /// <summary>Inserts or updates the row.</summary>
    /// <param name="setting">Values to save; the id is forced to <see cref="TlsSetting.SingletonId"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The saved row.</returns>
    Task<TlsSetting> SaveAsync(TlsSetting setting, CancellationToken ct = default);
}
