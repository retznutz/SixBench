using Microsoft.EntityFrameworkCore;
using SixBench.Data.Entities;

namespace SixBench.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ITlsSettingRepository"/>.
/// </summary>
/// <param name="db">The database context.</param>
public sealed class TlsSettingRepository(SixBenchDbContext db) : ITlsSettingRepository
{
    /// <inheritdoc />
    public Task<TlsSetting?> GetAsync(CancellationToken ct = default) =>
        db.TlsSettings.FirstOrDefaultAsync(s => s.Id == TlsSetting.SingletonId, ct);

    /// <inheritdoc />
    public async Task<TlsSetting> SaveAsync(TlsSetting setting, CancellationToken ct = default)
    {
        setting.Id = TlsSetting.SingletonId;
        if (db.Entry(setting).State == EntityState.Detached)
        {
            var existing = await GetAsync(ct);
            if (existing is null)
            {
                db.TlsSettings.Add(setting);
            }
            else
            {
                db.Entry(existing).CurrentValues.SetValues(setting);
                setting = existing;
            }
        }

        await db.SaveChangesAsync(ct);
        return setting;
    }
}
