using SixBench.Common.Dtos;
using SixBench.Common.Enums;
using SixBench.Data.Entities;
using SixBench.Data.Repositories;
using SixBench.Services.Exceptions;

namespace SixBench.Services.Roku;

/// <summary>
/// Developer tools for a saved Roku: SceneGraph dumps, channel performance and the channel registry.
/// </summary>
public interface IRokuDevToolsService
{
    /// <summary>Dumps SceneGraph nodes of the foreground channel.</summary>
    /// <param name="rokuId">Saved device id.</param>
    /// <param name="scope">Which nodes.</param>
    /// <param name="nodeId">Node id to match; required for <see cref="SgNodeScope.Nodes"/>.</param>
    /// <param name="includeSizes">Include each node's memory use.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The node tree.</returns>
    /// <exception cref="NotFoundException">No such device.</exception>
    /// <exception cref="ServiceValidationException">A node id is required but missing.</exception>
    /// <exception cref="RokuUnreachableException">The device did not respond.</exception>
    /// <exception cref="RokuRequestRejectedException">The device refused.</exception>
    Task<SgNodesDto> GetSgNodesAsync(int rokuId, SgNodeScope scope, string? nodeId, bool includeSizes, CancellationToken ct = default);

    /// <summary>Reads CPU and memory use of the foreground channel.</summary>
    /// <param name="rokuId">Saved device id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The sample.</returns>
    /// <exception cref="NotFoundException">No such device.</exception>
    /// <exception cref="RokuUnreachableException">The device did not respond.</exception>
    /// <exception cref="RokuRequestRejectedException">The device refused.</exception>
    Task<ChanPerfDto> GetChanPerfAsync(int rokuId, CancellationToken ct = default);

    /// <summary>Reads a channel's registry.</summary>
    /// <param name="rokuId">Saved device id.</param>
    /// <param name="appId">Channel id; <c>dev</c> for the sideloaded channel.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The registry.</returns>
    /// <exception cref="NotFoundException">No such device.</exception>
    /// <exception cref="ServiceValidationException">The app id is invalid.</exception>
    /// <exception cref="RokuUnreachableException">The device did not respond.</exception>
    /// <exception cref="RokuRequestRejectedException">The device refused.</exception>
    Task<RokuRegistryDto> GetRegistryAsync(int rokuId, string appId, CancellationToken ct = default);
}

/// <summary>
/// Default <see cref="IRokuDevToolsService"/>. Unlike remote control, these calls do not rediscover a
/// Roku that stopped responding: the performance view polls every second and must not trigger SSDP sweeps.
/// </summary>
/// <param name="repository">Roku repository.</param>
/// <param name="client">ECP developer-query client.</param>
public sealed class RokuDevToolsService(IRokuDeviceRepository repository, IRokuDevToolsClient client) : IRokuDevToolsService
{
    /// <inheritdoc />
    public async Task<SgNodesDto> GetSgNodesAsync(
        int rokuId, SgNodeScope scope, string? nodeId, bool includeSizes, CancellationToken ct = default)
    {
        if (scope == SgNodeScope.Nodes && string.IsNullOrWhiteSpace(nodeId))
        {
            throw new ServiceValidationException("A node id is required to search nodes by id.");
        }

        var device = await LoadAsync(rokuId, ct);
        return await client.GetSgNodesAsync(device.IpAddress, device.Port, scope, nodeId?.Trim(), includeSizes, ct);
    }

    /// <inheritdoc />
    public async Task<ChanPerfDto> GetChanPerfAsync(int rokuId, CancellationToken ct = default)
    {
        var device = await LoadAsync(rokuId, ct);
        return await client.GetChanPerfAsync(device.IpAddress, device.Port, ct);
    }

    /// <inheritdoc />
    public async Task<RokuRegistryDto> GetRegistryAsync(int rokuId, string appId, CancellationToken ct = default)
    {
        var trimmed = appId.Trim();
        if (trimmed.Length is 0 or > 64 || !trimmed.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-'))
        {
            throw new ServiceValidationException("App id must be 'dev' or a channel id (letters, digits, '_' or '-').");
        }

        var device = await LoadAsync(rokuId, ct);
        return await client.GetRegistryAsync(device.IpAddress, device.Port, trimmed, ct);
    }

    private async Task<RokuDevice> LoadAsync(int rokuId, CancellationToken ct) =>
        await repository.GetAsync(rokuId, ct) ?? throw new NotFoundException($"Roku device {rokuId} was not found.");
}
