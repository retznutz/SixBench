using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixBench.Common.Options;
using SixBench.Services.Exceptions;

namespace SixBench.Services.Roku;

/// <summary>
/// A Roku found on the network.
/// </summary>
/// <param name="Host">IP address.</param>
/// <param name="Port">ECP port.</param>
/// <param name="Info">Device info.</param>
public sealed record DiscoveredRoku(string Host, int Port, RokuDeviceInfo Info);

/// <summary>
/// Finds Roku devices on the LAN using SSDP.
/// </summary>
public interface IRokuDiscoveryService
{
    /// <summary>
    /// Sends an SSDP M-SEARCH and queries each responder's device info.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Devices that responded.</returns>
    Task<IReadOnlyList<DiscoveredRoku>> DiscoverAsync(CancellationToken ct = default);
}

/// <summary>
/// Default <see cref="IRokuDiscoveryService"/>.
/// </summary>
/// <param name="ecp">ECP client.</param>
/// <param name="options">Roku options.</param>
/// <param name="logger">Logger.</param>
public sealed class RokuDiscoveryService(
    IRokuEcpClient ecp,
    IOptions<RokuOptions> options,
    ILogger<RokuDiscoveryService> logger) : IRokuDiscoveryService
{
    private static readonly IPEndPoint MulticastEndpoint = new(IPAddress.Parse("239.255.255.250"), 1900);

    /// <inheritdoc />
    public async Task<IReadOnlyList<DiscoveredRoku>> DiscoverAsync(CancellationToken ct = default)
    {
        // A socket bound to 0.0.0.0 sends multicast out of whichever adapter has the lowest-metric
        // 224.0.0.0/4 route, which on multi-homed hosts (Hyper-V, WSL, VPN, Docker) is often not the LAN.
        // Search from every adapter instead.
        var interfaces = GetSearchAddresses();
        logger.LogDebug("SSDP: searching from {Addresses}", string.Join(", ", interfaces.Select(a => a.ToString())));

        var locations = new HashSet<Uri>();
        using (var window = CancellationTokenSource.CreateLinkedTokenSource(ct))
        {
            window.CancelAfter(options.Value.DiscoveryTimeoutMs);
            await Task.WhenAll(interfaces.Select(address => SearchAsync(address, locations, window.Token, ct)));
        }

        var lookups = locations.Select(async location =>
        {
            try
            {
                var info = await ecp.GetDeviceInfoAsync(location.Host, location.Port, ct);
                return new DiscoveredRoku(location.Host, location.Port, info);
            }
            catch (RokuUnreachableException ex)
            {
                logger.LogWarning("Discovered {Location} but device-info failed: {Message}", location, ex.Message);
                return null;
            }
        });

        var found = (await Task.WhenAll(lookups)).OfType<DiscoveredRoku>().ToList();
        logger.LogInformation("Roku discovery found {Count} device(s)", found.Count);
        return found;
    }

    /// <summary>
    /// Sends an M-SEARCH from one local address and collects Roku locations until the window closes.
    /// </summary>
    /// <param name="local">Local address to send from (<see cref="IPAddress.Any"/> lets the OS choose).</param>
    /// <param name="locations">Shared set of locations found so far.</param>
    /// <param name="window">Cancelled when the discovery window elapses.</param>
    /// <param name="ct">Caller's cancellation token.</param>
    private async Task SearchAsync(IPAddress local, HashSet<Uri> locations, CancellationToken window, CancellationToken ct)
    {
        UdpClient udp;
        try
        {
            udp = new UdpClient(new IPEndPoint(local, 0));
        }
        catch (SocketException ex)
        {
            logger.LogDebug("SSDP: cannot bind {Address}: {Message}", local, ex.Message);
            return;
        }

        using (udp)
        {
            try
            {
                if (!local.Equals(IPAddress.Any))
                {
                    udp.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, local.GetAddressBytes());
                }

                var request = Encoding.ASCII.GetBytes(SsdpResponseParser.RokuSearchRequest);

                // UDP is lossy; send twice.
                await udp.SendAsync(request, MulticastEndpoint, ct);
                await udp.SendAsync(request, MulticastEndpoint, ct);
            }
            catch (SocketException ex)
            {
                logger.LogDebug("SSDP: cannot search from {Address}: {Message}", local, ex.Message);
                return;
            }

            try
            {
                while (true)
                {
                    var result = await udp.ReceiveAsync(window);
                    var location = SsdpResponseParser.ParseLocation(Encoding.ASCII.GetString(result.Buffer));
                    if (location is null)
                    {
                        continue;
                    }

                    bool added;
                    lock (locations)
                    {
                        added = locations.Add(location);
                    }

                    if (added)
                    {
                        logger.LogDebug("SSDP: Roku at {Location} (via {Address})", location, local);
                    }
                }
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // Discovery window elapsed.
            }
            catch (SocketException ex)
            {
                logger.LogDebug("SSDP: receive failed on {Address}: {Message}", local, ex.Message);
            }
        }
    }

    /// <summary>
    /// Gets the IPv4 address of every active, multicast-capable, non-loopback adapter.
    /// </summary>
    /// <returns>The addresses, or <see cref="IPAddress.Any"/> alone if none qualify.</returns>
    private static IReadOnlyList<IPAddress> GetSearchAddresses()
    {
        var addresses = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up
                && n.NetworkInterfaceType != NetworkInterfaceType.Loopback
                && n.SupportsMulticast)
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .Select(u => u.Address)
            .Where(a => a.AddressFamily == AddressFamily.InterNetwork)
            .Distinct()
            .ToList();

        return addresses.Count > 0 ? addresses : [IPAddress.Any];
    }
}
