using System.Xml.Linq;
using SixBench.Common.Dtos;
using SixBench.Common.Enums;
using SixBench.Services.Exceptions;

namespace SixBench.Services.Roku;

/// <summary>
/// Typed-<see cref="HttpClient"/> implementation of <see cref="IRokuEcpClient"/>.
/// </summary>
/// <param name="http">HTTP client (timeout configured at registration).</param>
public sealed class RokuEcpClient(HttpClient http) : IRokuEcpClient
{
    /// <inheritdoc />
    public async Task<RokuDeviceInfo> GetDeviceInfoAsync(string host, int port, CancellationToken ct = default)
    {
        var uri = BuildUri(host, port, "query/device-info");
        var xml = await ExecuteAsync(host, () => http.GetStringAsync(uri, ct));
        try
        {
            return RokuDeviceInfo.Parse(xml);
        }
        catch (Exception ex) when (ex is FormatException or System.Xml.XmlException)
        {
            throw new RokuUnreachableException($"{host} responded but is not a Roku ECP device: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public Task SendKeyAsync(string host, int port, RokuKey key, KeyAction action, CancellationToken ct = default)
    {
        var verb = action switch
        {
            KeyAction.Down => "keydown",
            KeyAction.Up => "keyup",
            _ => "keypress",
        };
        return PostAsync(host, BuildUri(host, port, $"{verb}/{key}"), ct);
    }

    /// <inheritdoc />
    public Task SendLiteralAsync(string host, int port, string character, CancellationToken ct = default) =>
        PostAsync(host, BuildUri(host, port, $"keypress/Lit_{Uri.EscapeDataString(character)}"), ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RokuAppDto>> GetAppsAsync(string host, int port, CancellationToken ct = default)
    {
        var xml = await ExecuteAsync(host, () => http.GetStringAsync(BuildUri(host, port, "query/apps"), ct));
        try
        {
            return ParseApps(xml);
        }
        catch (System.Xml.XmlException ex)
        {
            throw new RokuUnreachableException($"{host} responded but not with ECP XML: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public Task LaunchAsync(string host, int port, string appId, CancellationToken ct = default) =>
        PostAsync(host, BuildUri(host, port, $"launch/{Uri.EscapeDataString(appId)}"), ct);

    /// <summary>
    /// Parses a <c>/query/apps</c> response.
    /// </summary>
    /// <param name="xml">Response body.</param>
    /// <returns>The channels.</returns>
    /// <exception cref="System.Xml.XmlException">The body is not XML.</exception>
    public static IReadOnlyList<RokuAppDto> ParseApps(string xml) =>
        XDocument.Parse(xml).Root?.Elements("app")
            .Select(a => new RokuAppDto((string?)a.Attribute("id") ?? string.Empty, a.Value.Trim(), (string?)a.Attribute("version")))
            .Where(a => a.Id.Length > 0)
            .ToList()
        ?? [];

    /// <summary>
    /// Builds an ECP URL.
    /// </summary>
    /// <param name="host">IP or host name.</param>
    /// <param name="port">ECP port.</param>
    /// <param name="path">Path relative to the ECP root (already escaped).</param>
    /// <returns>The URL.</returns>
    public static Uri BuildUri(string host, int port, string path) => new($"http://{host}:{port}/{path}");

    private async Task PostAsync(string host, Uri uri, CancellationToken ct)
    {
        using var response = await ExecuteAsync(host, () => http.PostAsync(uri, content: null, ct));
        if (!response.IsSuccessStatusCode)
        {
            throw new RokuUnreachableException(
                $"Roku {host} rejected {uri.AbsolutePath} with HTTP {(int)response.StatusCode}. " +
                "Check Settings > System > Advanced system settings > Control by mobile apps.");
        }
    }

    /// <summary>
    /// Runs an HTTP call, turning transport failures and timeouts into <see cref="RokuUnreachableException"/>.
    /// </summary>
    /// <typeparam name="T">Result type.</typeparam>
    /// <param name="host">Roku host, for messages.</param>
    /// <param name="call">The call.</param>
    /// <returns>The call's result.</returns>
    internal static async Task<T> ExecuteAsync<T>(string host, Func<Task<T>> call)
    {
        try
        {
            return await call();
        }
        catch (HttpRequestException ex)
        {
            throw new RokuUnreachableException($"Roku {host} is unreachable: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            throw new RokuUnreachableException($"Roku {host} did not respond in time.", ex);
        }
    }
}
