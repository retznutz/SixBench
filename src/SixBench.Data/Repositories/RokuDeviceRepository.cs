using Microsoft.EntityFrameworkCore;
using SixBench.Data.Entities;

namespace SixBench.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRokuDeviceRepository"/>.
/// </summary>
/// <param name="db">The database context.</param>
public sealed class RokuDeviceRepository(SixBenchDbContext db) : IRokuDeviceRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<RokuDevice>> ListAsync(CancellationToken ct = default) =>
        await db.RokuDevices.AsNoTracking().OrderBy(r => r.FriendlyName).ToListAsync(ct);

    /// <inheritdoc />
    public Task<RokuDevice?> GetAsync(int id, CancellationToken ct = default) =>
        db.RokuDevices.FirstOrDefaultAsync(r => r.Id == id, ct);

    /// <inheritdoc />
    public Task<RokuDevice?> GetBySerialAsync(string serialNumber, CancellationToken ct = default) =>
        db.RokuDevices.FirstOrDefaultAsync(r => r.SerialNumber == serialNumber, ct);

    /// <inheritdoc />
    public async Task<RokuDevice> UpsertBySerialAsync(RokuDevice device, CancellationToken ct = default)
    {
        var existing = await GetBySerialAsync(device.SerialNumber, ct);
        if (existing is null)
        {
            db.RokuDevices.Add(device);
            await db.SaveChangesAsync(ct);
            return device;
        }

        existing.FriendlyName = device.FriendlyName;
        existing.Model = device.Model ?? existing.Model;
        existing.IpAddress = device.IpAddress;
        existing.Port = device.Port;
        existing.LastSeenUtc = device.LastSeenUtc;
        existing.IsManual = existing.IsManual && device.IsManual;
        await db.SaveChangesAsync(ct);
        return existing;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var existing = await db.RokuDevices.Include(r => r.EncoderLinks).FirstOrDefaultAsync(r => r.Id == id, ct);
        if (existing is null)
        {
            return false;
        }

        foreach (var link in existing.EncoderLinks)
        {
            link.RokuDeviceId = null;
        }

        db.RokuDevices.Remove(existing);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
