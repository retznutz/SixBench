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

    private static async Task<T> ExecuteAsync<T>(string host, Func<Task<T>> call)
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
