using System.Net;
using DnsClient;
using Microsoft.Extensions.Logging;

namespace SixBench.Services.Certificates;

/// <summary>
/// Result of looking up a TXT record.
/// </summary>
/// <param name="Found">True if a record with the expected value exists.</param>
/// <param name="Values">Every TXT value found at the name.</param>
/// <param name="Source">Which servers answered, for messages.</param>
public sealed record DnsTxtCheckResult(bool Found, IReadOnlyList<string> Values, string Source);

/// <summary>
/// Checks whether a DNS TXT record is visible before Let's Encrypt is asked to validate it
/// (a failed validation uses up the order and is rate-limited).
/// </summary>
public interface IDnsTxtChecker
{
    /// <summary>
    /// Looks up the TXT records at <paramref name="name"/>.
    /// </summary>
    /// <param name="name">Record name, e.g. <c>_acme-challenge.roku.example.com</c>.</param>
    /// <param name="expectedValue">The value that must be present.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>What was found.</returns>
    Task<DnsTxtCheckResult> CheckAsync(string name, string expectedValue, CancellationToken ct = default);
}

/// <summary>
/// <see cref="IDnsTxtChecker"/> that asks the domain's authoritative nameservers directly, as Let's Encrypt does,
/// so a resolver that cached "no record" earlier can't hide a record that was just added.
/// Falls back to the system resolver if the authoritative servers can't be reached.
/// </summary>
/// <param name="logger">Logger.</param>
public sealed class DnsTxtChecker(ILogger<DnsTxtChecker> logger) : IDnsTxtChecker
{
    private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(5);

    /// <inheritdoc />
    public async Task<DnsTxtCheckResult> CheckAsync(string name, string expectedValue, CancellationToken ct = default)
    {
        var system = new LookupClient(new LookupClientOptions { UseCache = false, Timeout = QueryTimeout, Retries = 1 });
        try
        {
            var (servers, hosts) = await FindAuthoritativeServersAsync(system, name, ct);

            // Ask every nameserver separately (one LookupClient over all of them only fails over) and require all of them
            // to serve the value: Let's Encrypt validates from several places, and a lagging server fails the order.
            var answers = await Task.WhenAll(servers.Select(s => QueryServerAsync(s, name, ct)));
            var answered = answers.OfType<IDnsQueryResponse>().ToList();

            // A CNAME (delegated validation) needs following; let the recursive resolver do it.
            if (answered.Count > 0 && !answered.Any(r => r.Answers.CnameRecords().Any()))
            {
                var perServer = answered.Select(r => r.Answers.TxtRecords().SelectMany(t => t.Text).ToList()).ToList();
                var source = $"the domain's nameservers ({string.Join(", ", hosts)})";
                var values = perServer.SelectMany(v => v).Distinct(StringComparer.Ordinal).ToList();
                return new(perServer.All(v => v.Contains(expectedValue, StringComparer.Ordinal)), values, source);
            }
        }
        catch (Exception ex) when (ex is DnsResponseException or System.Net.Sockets.SocketException or TimeoutException)
        {
            logger.LogDebug(ex, "Authoritative TXT lookup for {Name} failed; using the system resolver", name);
        }

        var fallback = await system.QueryAsync(name, QueryType.TXT, QueryClass.IN, ct);
        return Result(fallback.Answers.TxtRecords().SelectMany(r => r.Text).ToList(), expectedValue, "this server's DNS resolver");
    }

    /// <summary>Asks one authoritative server; null if it can't be reached (it may only be reachable over IPv6).</summary>
    private async Task<IDnsQueryResponse?> QueryServerAsync(NameServer server, string name, CancellationToken ct)
    {
        var client = new LookupClient(new LookupClientOptions(server)
        {
            UseCache = false,
            Recursion = false,
            Timeout = QueryTimeout,
            Retries = 1,
        });
        try
        {
            return await client.QueryAsync(name, QueryType.TXT, QueryClass.IN, ct);
        }
        catch (Exception ex) when (ex is DnsResponseException or System.Net.Sockets.SocketException or TimeoutException)
        {
            logger.LogDebug(ex, "TXT lookup for {Name} on {Server} failed", name, server);
            return null;
        }
    }

    private static DnsTxtCheckResult Result(List<string> values, string expected, string source) =>
        new(values.Contains(expected, StringComparer.Ordinal), values, source);

    /// <summary>Walks up from the record name to the zone that has NS records, and resolves those servers.</summary>
    private static async Task<(List<NameServer> Servers, List<string> Hosts)> FindAuthoritativeServersAsync(
        LookupClient resolver, string name, CancellationToken ct)
    {
        var labels = name.TrimEnd('.').Split('.');
        for (var i = 0; i < labels.Length - 1; i++)
        {
            var zone = string.Join('.', labels[i..]);
            var ns = await resolver.QueryAsync(zone, QueryType.NS, QueryClass.IN, ct);
            var hosts = ns.Answers.NsRecords().Select(r => r.NSDName.Value.TrimEnd('.')).Distinct().ToList();
            if (hosts.Count == 0)
            {
                continue;
            }

            var servers = new List<NameServer>();
            foreach (var host in hosts)
            {
                var a = await resolver.QueryAsync(host, QueryType.A, QueryClass.IN, ct);
                servers.AddRange(a.Answers.ARecords().Select(r => new NameServer(r.Address)));
            }

            return (servers, hosts);
        }

        return ([], []);
    }
}
