using Microsoft.EntityFrameworkCore;
using SixBench.Data.Entities;

namespace SixBench.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IEncoderLinkRepository"/>.
/// </summary>
/// <param name="db">The database context.</param>
public sealed class EncoderLinkRepository(SixBenchDbContext db) : IEncoderLinkRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<EncoderLink>> ListAsync(CancellationToken ct = default) =>
        await db.EncoderLinks.AsNoTracking().Include(l => l.RokuDevice).OrderBy(l => l.DisplayName).ToListAsync(ct);

    /// <inheritdoc />
    public Task<EncoderLink?> GetByStableIdAsync(string stableId, CancellationToken ct = default) =>
        db.EncoderLinks.Include(l => l.RokuDevice).FirstOrDefaultAsync(l => l.CaptureDeviceStableId == stableId, ct);

    /// <inheritdoc />
    public async Task<EncoderLink> SaveAsync(EncoderLink link, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        link.UpdatedUtc = now;
        if (link.Id == 0)
        {
            link.CreatedUtc = now;
            db.EncoderLinks.Add(link);
        }

        await db.SaveChangesAsync(ct);
        await db.Entry(link).Reference(l => l.RokuDevice).LoadAsync(ct);
        return link;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string stableId, CancellationToken ct = default)
    {
        var deleted = await db.EncoderLinks.Where(l => l.CaptureDeviceStableId == stableId).ExecuteDeleteAsync(ct);
        return deleted > 0;
    }
}
