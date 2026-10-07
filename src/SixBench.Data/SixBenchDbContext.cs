using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SixBench.Data.Entities;

namespace SixBench.Data;

/// <summary>
/// EF Core context for SixBench, including the ASP.NET Core Identity tables. Table names are singular.
/// </summary>
/// <param name="options">Context options.</param>
public class SixBenchDbContext(DbContextOptions<SixBenchDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<int>, int>(options)
{
    /// <summary>Known Roku devices.</summary>
    public DbSet<RokuDevice> RokuDevices => Set<RokuDevice>();

    /// <summary>Encoder-to-Roku links.</summary>
    public DbSet<EncoderLink> EncoderLinks => Set<EncoderLink>();

    /// <summary>HTTPS settings saved from the web app (a single row).</summary>
    public DbSet<TlsSetting> TlsSettings => Set<TlsSetting>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Identity tables, renamed from AspNet* to singular names.
        modelBuilder.Entity<AppUser>().ToTable("User");
        modelBuilder.Entity<IdentityRole<int>>().ToTable("Role");
        modelBuilder.Entity<IdentityUserRole<int>>().ToTable("UserRole");
        modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("UserClaim");
        modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("UserLogin");
        modelBuilder.Entity<IdentityUserToken<int>>().ToTable("UserToken");
        modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaim");

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

        modelBuilder.Entity<TlsSetting>(e =>
        {
            e.ToTable("TlsSetting");
            e.Property(x => x.Id).ValueGeneratedNever();
        });
    }
}
