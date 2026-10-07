using System.Text;
using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using SixBench.Services.Exceptions;

namespace SixBench.Services.Certificates;

/// <summary>
/// An open ACME order for one domain, verified with a DNS-01 challenge.
/// </summary>
public interface IAcmeDnsOrder
{
    /// <summary>Value for the <c>_acme-challenge</c> TXT record, or null if Let's Encrypt already trusts this account for the domain.</summary>
    string? RecordValue { get; }

    /// <summary>Asks Let's Encrypt to check the TXT record and waits for the result.</summary>
    /// <param name="timeout">How long to wait.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that fails if validation fails or times out.</returns>
    Task ValidateAsync(TimeSpan timeout, CancellationToken ct = default);

    /// <summary>Finalizes the order and returns the certificate with its private key as a PFX (no password).</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>PFX bytes.</returns>
    Task<byte[]> IssuePfxAsync(CancellationToken ct = default);
}

/// <summary>
/// Talks to Let's Encrypt.
/// </summary>
public interface IAcmeClient
{
    /// <summary>
    /// Loads the ACME account key (creating and saving it on first use), registers the account and opens an order.
    /// </summary>
    /// <param name="domain">Domain name.</param>
    /// <param name="email">Account contact email.</param>
    /// <param name="accountKeyPath">Where the account key is kept.</param>
    /// <param name="useStaging">Use the staging server.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The order.</returns>
    Task<IAcmeDnsOrder> CreateOrderAsync(string domain, string email, string accountKeyPath, bool useStaging, CancellationToken ct = default);
}

/// <summary>
/// <see cref="IAcmeClient"/> built on Certes.
/// </summary>
public sealed class CertesAcmeClient : IAcmeClient
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);

    /// <inheritdoc />
    public async Task<IAcmeDnsOrder> CreateOrderAsync(string domain, string email, string accountKeyPath, bool useStaging, CancellationToken ct = default)
    {
        var server = useStaging ? WellKnownServers.LetsEncryptStagingV2 : WellKnownServers.LetsEncryptV2;
        var existingKey = File.Exists(accountKeyPath) ? await File.ReadAllTextAsync(accountKeyPath, ct) : null;
        var acme = existingKey is null ? new AcmeContext(server) : new AcmeContext(server, KeyFactory.FromPem(existingKey));

        // Returns the existing account for this key, or creates it (also on a different server).
        await Wrap(() => acme.NewAccount(email, termsOfServiceAgreed: true));
        if (existingKey is null)
        {
            SecureFile.Write(accountKeyPath, Encoding.ASCII.GetBytes(acme.AccountKey.ToPem()));
        }

        var order = await Wrap(() => acme.NewOrder([domain]));
        var authorization = (await Wrap(order.Authorizations)).First();
        var alreadyValid = (await Wrap(authorization.Resource)).Status == AuthorizationStatus.Valid;
        var challenge = alreadyValid ? null : await Wrap(authorization.Dns)
            ?? throw new CertificateRequestException($"Let's Encrypt did not offer a DNS challenge for {domain}.");

        return new Order(domain, order, authorization, challenge, challenge is null ? null : acme.AccountKey.DnsTxt(challenge.Token));
    }

    private static async Task<T> Wrap<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (AcmeRequestException ex)
        {
            throw new CertificateRequestException($"Let's Encrypt refused the request: {ex.Error?.Detail ?? ex.Message}", ex);
        }
        catch (AcmeException ex)
        {
            throw new CertificateRequestException($"Let's Encrypt request failed: {ex.Message}", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new CertificateRequestException($"Could not reach Let's Encrypt: {ex.Message}", ex);
        }
    }

    private sealed class Order(
        string domain, IOrderContext order, IAuthorizationContext authorization, IChallengeContext? challenge, string? recordValue) : IAcmeDnsOrder
    {
        public string? RecordValue => recordValue;

        public async Task ValidateAsync(TimeSpan timeout, CancellationToken ct = default)
        {
            if (challenge is null)
            {
                return;
            }

            await Wrap(challenge.Validate);
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                await Task.Delay(PollInterval, ct);
                var state = await Wrap(authorization.Resource);
                if (state.Status == AuthorizationStatus.Valid)
                {
                    return;
                }

                if (state.Status == AuthorizationStatus.Invalid)
                {
                    var detail = state.Challenges?.FirstOrDefault(c => c.Error is not null)?.Error?.Detail;
                    throw new CertificateRequestException(
                        $"Domain validation failed{(detail is null ? "." : $": {detail}")} Check the DNS credentials and that the TXT record was set.");
                }
            }

            throw new CertificateRequestException($"Domain validation timed out after {timeout.TotalSeconds:0} seconds.");
        }

        public async Task<byte[]> IssuePfxAsync(CancellationToken ct = default)
        {
            var privateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
            var chain = await Wrap(() => order.Generate(new CsrInfo { CommonName = domain }, privateKey));
            ct.ThrowIfCancellationRequested();
            return chain.ToPfx(privateKey).Build(domain, string.Empty);
        }
    }
}
