using System.Net;
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
        var locations = new HashSet<Uri>();
        using (var udp = new UdpClient(new IPEndPoint(IPAddress.Any, 0)))
        {
            var request = Encoding.ASCII.GetBytes(SsdpResponseParser.RokuSearchRequest);

            // UDP is lossy; send twice.
            await udp.SendAsync(request, MulticastEndpoint, ct);
            await udp.SendAsync(request, MulticastEndpoint, ct);

            using var window = CancellationTokenSource.CreateLinkedTokenSource(ct);
            window.CancelAfter(options.Value.DiscoveryTimeoutMs);
            try
            {
                while (true)
                {
                    var result = await udp.ReceiveAsync(window.Token);
                    var location = SsdpResponseParser.ParseLocation(Encoding.ASCII.GetString(result.Buffer));
                    if (location is not null && locations.Add(location))
                    {
                        logger.LogDebug("SSDP: Roku at {Location}", location);
                    }
                }
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // Discovery window elapsed.
            }
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
}
