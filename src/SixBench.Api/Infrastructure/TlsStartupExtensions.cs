using Microsoft.Data.Sqlite;
using Serilog;
using Serilog.Extensions.Logging;
using SixBench.Common.Options;
using SixBench.Data;
using SixBench.Services.Certificates;

namespace SixBench.Api.Infrastructure;

/// <summary>
/// Binds Kestrel to <c>Server:Port</c>: HTTPS when TLS is enabled and the certificate exists, otherwise HTTP.
/// The certificate is served through <see cref="IServerCertificate"/>, so renewals take effect without a restart.
/// </summary>
public static class TlsStartupExtensions
{
    /// <summary>
    /// Configures the listener and registers <see cref="IServerCertificate"/> and <see cref="TlsRuntimeState"/>.
    /// </summary>
    /// <param name="builder">The app builder.</param>
    /// <returns>How the server is bound.</returns>
    public static TlsRuntimeState ConfigureTls(this WebApplicationBuilder builder)
    {
        var server = builder.Configuration.GetSection(ServerOptions.SectionName).Get<ServerOptions>() ?? new ServerOptions();
        var tls = builder.Configuration.GetSection(TlsOptions.SectionName).Get<TlsOptions>() ?? new TlsOptions();
        var certificate = new ServerCertificate(new SerilogLoggerFactory().CreateLogger<ServerCertificate>());
        builder.Services.AddSingleton<IServerCertificate>(certificate);

        var enabled = tls.Enabled || ReadDatabaseOverride(builder.Configuration, builder.Environment.ContentRootPath);
        var certPath = CertificatePaths.Certificate(tls, builder.Environment.ContentRootPath);
        var https = false;
        if (!enabled)
        {
            Log.Information("TLS is not enabled; serving HTTP on port {Port}", server.Port);
        }
        else if (!File.Exists(certPath))
        {
            Log.Warning("TLS is enabled but the certificate was not found at {CertPath}; serving HTTP on port {Port}", certPath, server.Port);
        }
        else
        {
            Log.Information("TLS enabled, loading certificate from {CertPath}", certPath);
            https = certificate.Load(certPath);
        }

        builder.WebHost.ConfigureKestrel(kestrel =>
        {
            if (https)
            {
                kestrel.ListenAnyIP(server.Port, o => o.UseHttps(h => h.ServerCertificateSelector = (_, _) => certificate.Current));
            }
            else
            {
                kestrel.ListenAnyIP(server.Port);
            }
        });

        var state = new TlsRuntimeState(https, server.Port);
        builder.Services.AddSingleton(state);
        return state;
    }

    /// <summary>
    /// Reads <c>TlsSetting.Enabled</c> straight from the database (DI isn't available yet).
    /// False if the database or table doesn't exist yet (first run, migration pending).
    /// </summary>
    /// <param name="configuration">App configuration.</param>
    /// <param name="contentRoot">App folder.</param>
    /// <returns>True if HTTPS was enabled from the web app.</returns>
    public static bool ReadDatabaseOverride(IConfiguration configuration, string contentRoot)
    {
        try
        {
            var connectionString = DataServiceCollectionExtensions.ResolveConnectionString(
                configuration.GetConnectionString(DataServiceCollectionExtensions.ConnectionStringName) ?? "Data Source=data/sixbench.db",
                contentRoot);
            var readOnly = new SqliteConnectionStringBuilder(connectionString) { Mode = SqliteOpenMode.ReadOnly, Pooling = false };
            using var connection = new SqliteConnection(readOnly.ToString());
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = """SELECT "Enabled" FROM "TlsSetting" WHERE "Id" = 1""";
            return command.ExecuteScalar() is long enabled && enabled != 0;
        }
        catch (SqliteException)
        {
            return false;
        }
    }
}
