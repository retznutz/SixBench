using Microsoft.Extensions.Options;
using SixBench.Common.Dtos;
using SixBench.Common.Options;
using SixBench.Data.Entities;
using SixBench.Data.Repositories;
using SixBench.Services.Exceptions;
using SixBench.Services.Mapping;

namespace SixBench.Services.Roku;

/// <summary>
/// Default <see cref="IRokuDeviceService"/>.
/// </summary>
/// <param name="repository">Roku repository.</param>
/// <param name="discovery">SSDP discovery.</param>
/// <param name="ecp">ECP client.</param>
/// <param name="options">Roku options.</param>
public sealed class RokuDeviceService(
    IRokuDeviceRepository repository,
    IRokuDiscoveryService discovery,
    IRokuEcpClient ecp,
    IOptions<RokuOptions> options) : IRokuDeviceService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<RokuDeviceDto>> ListAsync(CancellationToken ct = default) =>
        (await repository.ListAsync(ct)).Select(r => r.ToDto()).ToList();

    /// <inheritdoc />
    public async Task<RokuDeviceDto> GetAsync(int id, CancellationToken ct = default) =>
        (await repository.GetAsync(id, ct))?.ToDto() ?? throw new NotFoundException($"Roku device {id} was not found.");

    /// <inheritdoc />
    public async Task<IReadOnlyList<RokuDeviceDto>> DiscoverAsync(CancellationToken ct = default)
    {
        var found = await discovery.DiscoverAsync(ct);
        var saved = new List<RokuDeviceDto>();
        foreach (var device in found)
        {
            var entity = await repository.UpsertBySerialAsync(ToEntity(device.Host, device.Port, device.Info, isManual: false), ct);
            saved.Add(entity.ToDto());
        }

        return saved;
    }

    /// <inheritdoc />
    public async Task<RokuDeviceDto> AddManualAsync(string host, int? port, CancellationToken ct = default)
    {
        var trimmed = host.Trim();
        if (Uri.CheckHostName(trimmed) == UriHostNameType.Unknown)
        {
            throw new ServiceValidationException($"'{host}' is not a valid IP address or host name.");
        }

        var ecpPort = port ?? options.Value.EcpPort;
        var info = await ecp.GetDeviceInfoAsync(trimmed, ecpPort, ct);
        var entity = await repository.UpsertBySerialAsync(ToEntity(trimmed, ecpPort, info, isManual: true), ct);
        return entity.ToDto();
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(int id, CancellationToken ct = default) => repository.DeleteAsync(id, ct);

    private static RokuDevice ToEntity(string host, int port, RokuDeviceInfo info, bool isManual) => new()
    {
        SerialNumber = info.SerialNumber,
        FriendlyName = info.FriendlyName,
        Model = info.Model,
        IpAddress = host,
        Port = port,
        IsManual = isManual,
        LastSeenUtc = DateTime.UtcNow,
    };
}
