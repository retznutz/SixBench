using Microsoft.EntityFrameworkCore;
using SixBench.Data.Entities;

namespace SixBench.Data;

/// <summary>
/// EF Core context for SixBench. Table names are singular.
/// </summary>
/// <param name="options">Context options.</param>
public class SixBenchDbContext(DbContextOptions<SixBenchDbContext> options) : DbContext(options)
{
    /// <summary>Known Roku devices.</summary>
    public DbSet<RokuDevice> RokuDevices => Set<RokuDevice>();

    /// <summary>Encoder-to-Roku links.</summary>
    public DbSet<EncoderLink> EncoderLinks => Set<EncoderLink>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RokuDevice>(e =>
        {
            e.ToTable("RokuDevice");
            e.HasIndex(x => x.SerialNumber).IsUnique();
        });

        modelBuilder.Entity<EncoderLink>(e =>
        {
            e.ToTable("EncoderLink");
            e.HasIndex(x => x.CaptureDeviceStableId).IsUnique();
            e.HasOne(x => x.RokuDevice)
                .WithMany(r => r.EncoderLinks)
                .HasForeignKey(x => x.RokuDeviceId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
