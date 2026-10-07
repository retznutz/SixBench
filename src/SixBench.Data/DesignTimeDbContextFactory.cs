using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SixBench.Data;

/// <summary>
/// Lets <c>dotnet ef</c> create the context without starting the API.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SixBenchDbContext>
{
    /// <inheritdoc />
    public SixBenchDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SixBenchDbContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options;
        return new SixBenchDbContext(options);
    }
}
