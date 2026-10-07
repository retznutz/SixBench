using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SixBench.Common.Utilities;
using SixBench.Data.Repositories;

namespace SixBench.Data;

/// <summary>
/// Registers the SixBench data layer.
/// </summary>
public static class DataServiceCollectionExtensions
{
    /// <summary>Name of the connection string in configuration.</summary>
    public const string ConnectionStringName = "SixBench";

    /// <summary>
    /// Adds the <see cref="SixBenchDbContext"/> (SQLite) and repositories.
    /// A relative SQLite path is resolved against <paramref name="contentRoot"/> and its directory is created.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">App configuration containing <c>ConnectionStrings:SixBench</c>.</param>
    /// <param name="contentRoot">Base directory for relative database paths.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddSixBenchData(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRoot)
    {
        var connectionString = ResolveConnectionString(
            configuration.GetConnectionString(ConnectionStringName) ?? "Data Source=data/sixbench.db",
            contentRoot);

        services.AddDbContext<SixBenchDbContext>(o => o.UseSqlite(connectionString));
        services.AddScoped<IRokuDeviceRepository, RokuDeviceRepository>();
        services.AddScoped<IEncoderLinkRepository, EncoderLinkRepository>();
        return services;
    }

    /// <summary>
    /// Makes the SQLite data source absolute (forward slashes) and ensures its directory exists.
    /// In-memory data sources are returned unchanged.
    /// </summary>
    /// <param name="connectionString">The configured connection string.</param>
    /// <param name="contentRoot">Base directory for relative paths.</param>
    /// <returns>The resolved connection string.</returns>
    public static string ResolveConnectionString(string connectionString, string contentRoot)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.DataSource)
            || builder.DataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase)
            || builder.Mode == SqliteOpenMode.Memory)
        {
            return connectionString;
        }

        var fullPath = PathUtil.ResolveAgainst(builder.DataSource, contentRoot);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        builder.DataSource = fullPath;
        return builder.ToString();
    }
}
