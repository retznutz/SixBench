using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixBench.Common.Enums;
using SixBench.Common.Options;
using SixBench.Data.Entities;
using SixBench.Data.Repositories;
using SixBench.Services.Exceptions;

namespace SixBench.Services.Roku;

/// <summary>
/// Default <see cref="IRokuControlService"/>. If a discovered Roku stops responding, it is rediscovered once
/// by serial number (DHCP may have given it a new address) and the command is retried.
/// </summary>
/// <param name="repository">Roku repository.</param>
/// <param name="ecp">ECP client.</param>
/// <param name="discovery">SSDP discovery.</param>
/// <param name="options">Roku options.</param>
/// <param name="logger">Logger.</param>
public sealed class RokuControlService(
    IRokuDeviceRepository repository,
    IRokuEcpClient ecp,
    IRokuDiscoveryService discovery,
    IOptions<RokuOptions> options,
    ILogger<RokuControlService> logger) : IRokuControlService
{
    /// <inheritdoc />
    public async Task SendKeyAsync(int rokuId, RokuKey key, KeyAction action, CancellationToken ct = default)
    {
        var device = await LoadAsync(rokuId, ct);
        await WithRediscoveryAsync(device, d => ecp.SendKeyAsync(d.IpAddress, d.Port, key, action, ct), ct);
    }

    /// <inheritdoc />
    public async Task SendTextAsync(int rokuId, string text, CancellationToken ct = default)
    {
        var device = await LoadAsync(rokuId, ct);
        var delay = TimeSpan.FromMilliseconds(Math.Max(0, options.Value.TextCharDelayMs));
        var first = true;
        foreach (var rune in text.EnumerateRunes())
        {
            var character = rune.ToString();
            if (first)
            {
                // Only the first character may trigger rediscovery; after that the address is known good.
                await WithRediscoveryAsync(device, d => ecp.SendLiteralAsync(d.IpAddress, d.Port, character, ct), ct);
                first = false;
            }
            else
            {
                await ecp.SendLiteralAsync(device.IpAddress, device.Port, character, ct);
            }

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, ct);
            }
        }
    }

    private async Task<RokuDevice> LoadAsync(int rokuId, CancellationToken ct) =>
        await repository.GetAsync(rokuId, ct) ?? throw new NotFoundException($"Roku device {rokuId} was not found.");

    private async Task WithRediscoveryAsync(RokuDevice device, Func<RokuDevice, Task> send, CancellationToken ct)
    {
        try
        {
            await send(device);
        }
        catch (RokuUnreachableException ex) when (ex.InnerException is not null)
        {
            logger.LogWarning("Roku {Name} at {Ip} unreachable; rediscovering by serial", device.FriendlyName, device.IpAddress);
            var found = (await discovery.DiscoverAsync(ct))
                .FirstOrDefault(d => d.Info.SerialNumber == device.SerialNumber);
            if (found is null || (found.Host == device.IpAddress && found.Port == device.Port))
            {
                throw;
            }

            logger.LogInformation("Roku {Name} moved from {OldIp} to {NewIp}", device.FriendlyName, device.IpAddress, found.Host);
            device.IpAddress = found.Host;
            device.Port = found.Port;
            device.LastSeenUtc = DateTime.UtcNow;
            await repository.UpsertBySerialAsync(device, ct);
            await send(device);
        }
    }
}
