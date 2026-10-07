using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SixBench.Data;
using SixBench.Data.Entities;
using SixBench.Data.Repositories;

namespace SixBench.Tests.Data;

public sealed class RepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly SixBenchDbContext _db;

    public RepositoryTests()
    {
        _connection.Open();
        _db = new SixBenchDbContext(new DbContextOptionsBuilder<SixBenchDbContext>().UseSqlite(_connection).Options);
        _db.Database.Migrate();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private static RokuDevice Roku(string serial, string ip, bool manual = false) => new()
    {
        SerialNumber = serial,
        FriendlyName = "Living Room",
        IpAddress = ip,
        Port = 8060,
        IsManual = manual,
        LastSeenUtc = DateTime.UtcNow,
    };

    [Fact]
    public void Tables_use_singular_names()
    {
        var tables = _db.Model.GetEntityTypes().Select(e => e.GetTableName()).Order();

        Assert.Equal(["EncoderLink", "RokuDevice", "Role", "RoleClaim", "TlsSetting", "User", "UserClaim", "UserLogin", "UserRole", "UserToken"], tables);
    }

    [Fact]
    public async Task Upsert_by_serial_updates_ip_and_keeps_manual_flag_until_rediscovered()
    {
        var repo = new RokuDeviceRepository(_db);
        var first = await repo.UpsertBySerialAsync(Roku("S1", "10.0.0.2", manual: true));

        var updated = await repo.UpsertBySerialAsync(Roku("S1", "10.0.0.9", manual: false));

        Assert.Equal(first.Id, updated.Id);
        Assert.Equal("10.0.0.9", updated.IpAddress);
        Assert.False(updated.IsManual);
        Assert.Single(await repo.ListAsync());
    }

    [Fact]
    public async Task Deleting_a_roku_unlinks_its_encoders()
    {
        var rokus = new RokuDeviceRepository(_db);
        var links = new EncoderLinkRepository(_db);
        var roku = await rokus.UpsertBySerialAsync(Roku("S2", "10.0.0.3"));
        await links.SaveAsync(new EncoderLink { CaptureDeviceStableId = "avf:1", DisplayName = "Den", VideoInput = "cam", RokuDeviceId = roku.Id });

        Assert.True(await rokus.DeleteAsync(roku.Id));

        var link = await links.GetByStableIdAsync("avf:1");
        Assert.NotNull(link);
        Assert.Null(link.RokuDeviceId);
    }

    [Fact]
    public async Task Save_stamps_timestamps_and_loads_roku()
    {
        var roku = await new RokuDeviceRepository(_db).UpsertBySerialAsync(Roku("S3", "10.0.0.4"));
        var links = new EncoderLinkRepository(_db);

        var saved = await links.SaveAsync(new EncoderLink { CaptureDeviceStableId = "avf:2", DisplayName = "Den", VideoInput = "cam", RokuDeviceId = roku.Id });

        Assert.NotEqual(default, saved.CreatedUtc);
        Assert.Equal("S3", saved.RokuDevice?.SerialNumber);
        Assert.True(await links.DeleteAsync("avf:2"));
        Assert.False(await links.DeleteAsync("avf:2"));
    }

    [Fact]
    public void Relative_sqlite_paths_resolve_against_content_root()
    {
        var root = Path.Combine(Path.GetTempPath(), "sixbench-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var cs = DataServiceCollectionExtensions.ResolveConnectionString("Data Source=data/x.db", root);

            Assert.Contains(root.Replace('\\', '/') + "/data/x.db", cs);
            Assert.True(Directory.Exists(Path.Combine(root, "data")));
            Assert.Equal("Data Source=:memory:", DataServiceCollectionExtensions.ResolveConnectionString("Data Source=:memory:", root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
