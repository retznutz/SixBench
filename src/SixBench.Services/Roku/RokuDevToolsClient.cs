using System.Net;
using SixBench.Common.Dtos;
using SixBench.Common.Enums;
using SixBench.Services.Exceptions;

namespace SixBench.Services.Roku;

/// <summary>
/// ECP developer queries. These need developer mode on the Roku and are kept apart from
/// <see cref="IRokuEcpClient"/> because they get a longer timeout (SceneGraph dumps can be large).
/// </summary>
public interface IRokuDevToolsClient
{
    /// <summary>
    /// Dumps SceneGraph nodes of the foreground channel (<c>/query/sgnodes/{all|roots|nodes}</c>).
    /// </summary>
    /// <param name="host">IP or host name.</param>
    /// <param name="port">ECP port.</param>
    /// <param name="scope">Which nodes.</param>
    /// <param name="nodeId">Node id to match; required when <paramref name="scope"/> is <see cref="SgNodeScope.Nodes"/>.</param>
    /// <param name="includeSizes">Ask the Roku to report each node's memory use.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The node tree.</returns>
    /// <exception cref="RokuUnreachableException">The device did not respond.</exception>
    /// <exception cref="RokuRequestRejectedException">The device refused (developer mode off, no dev channel running).</exception>
    Task<SgNodesDto> GetSgNodesAsync(string host, int port, SgNodeScope scope, string? nodeId, bool includeSizes, CancellationToken ct = default);

    /// <summary>
    /// Reads CPU and memory use of the foreground channel (<c>/query/chanperf</c>).
    /// </summary>
    /// <param name="host">IP or host name.</param>
    /// <param name="port">ECP port.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The sample.</returns>
    /// <exception cref="RokuUnreachableException">The device did not respond.</exception>
    /// <exception cref="RokuRequestRejectedException">The device refused.</exception>
    Task<ChanPerfDto> GetChanPerfAsync(string host, int port, CancellationToken ct = default);

    /// <summary>
    /// Reads a channel's registry (<c>/query/registry/{appId}</c>).
    /// </summary>
    /// <param name="host">IP or host name.</param>
    /// <param name="port">ECP port.</param>
    /// <param name="appId">Channel id; <c>dev</c> for the sideloaded channel.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The registry.</returns>
    /// <exception cref="RokuUnreachableException">The device did not respond.</exception>
    /// <exception cref="RokuRequestRejectedException">The device refused.</exception>
    Task<RokuRegistryDto> GetRegistryAsync(string host, int port, string appId, CancellationToken ct = default);
}

/// <summary>
/// Typed-<see cref="HttpClient"/> implementation of <see cref="IRokuDevToolsClient"/>.
/// </summary>
/// <param name="http">HTTP client (timeout configured at registration).</param>
/// <param name="time">Clock.</param>
public sealed class RokuDevToolsClient(HttpClient http, TimeProvider time) : IRokuDevToolsClient
{
    /// <inheritdoc />
    public async Task<SgNodesDto> GetSgNodesAsync(
        string host, int port, SgNodeScope scope, string? nodeId, bool includeSizes, CancellationToken ct = default)
    {
        var path = scope switch
        {
            SgNodeScope.Roots => "query/sgnodes/roots",
            SgNodeScope.Nodes => $"query/sgnodes/nodes?node-id={Uri.EscapeDataString(nodeId ?? string.Empty)}",
            _ => "query/sgnodes/all",
        };
        if (includeSizes)
        {
            path += (path.Contains('?') ? "&" : "?") + "sizes=true";
        }

        var xml = await GetAsync(host, port, path, ct);
        return Parse(host, () => RokuDevToolsParser.ParseSgNodes(xml, scope, time.GetUtcNow().UtcDateTime));
    }

    /// <inheritdoc />
    public async Task<ChanPerfDto> GetChanPerfAsync(string host, int port, CancellationToken ct = default)
    {
        var xml = await GetAsync(host, port, "query/chanperf", ct);
        return Parse(host, () => RokuDevToolsParser.ParseChanPerf(xml));
    }

    /// <inheritdoc />
    public async Task<RokuRegistryDto> GetRegistryAsync(string host, int port, string appId, CancellationToken ct = default)
    {
        var xml = await GetAsync(host, port, $"query/registry/{Uri.EscapeDataString(appId)}", ct);
        return Parse(host, () => RokuDevToolsParser.ParseRegistry(xml, appId));
    }

    private async Task<string> GetAsync(string host, int port, string path, CancellationToken ct)
    {
        var uri = RokuEcpClient.BuildUri(host, port, path);
        using var response = await RokuEcpClient.ExecuteAsync(host, () => http.GetAsync(uri, ct));
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new RokuRequestRejectedException(
                $"Roku {host} refused {uri.AbsolutePath} (HTTP 403). Developer tools need developer mode enabled on the Roku " +
                "and Settings > System > Advanced system settings > Control by mobile apps set to Enabled.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new RokuRequestRejectedException(
                $"Roku {host} answered {uri.AbsolutePath} with HTTP {(int)response.StatusCode}. " +
                "This Roku OS version may not support that query.");
        }

        return await RokuEcpClient.ExecuteAsync(host, () => response.Content.ReadAsStringAsync(ct));
    }

    private static T Parse<T>(string host, Func<T> parse)
    {
        try
        {
            return parse();
        }
        catch (FormatException ex)
        {
            throw new RokuUnreachableException($"{host} responded but not with ECP XML: {ex.Message}", ex);
        }
    }
}
