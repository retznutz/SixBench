using SixBench.Common.Dtos;
using SixBench.Common.Enums;

namespace SixBench.Services.Roku;

/// <summary>
/// Low-level Roku External Control Protocol client (HTTP on port 8060).
/// </summary>
public interface IRokuEcpClient
{
    /// <summary>
    /// Queries <c>/query/device-info</c>.
    /// </summary>
    /// <param name="host">IP or host name.</param>
    /// <param name="port">ECP port.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Device info.</returns>
    /// <exception cref="Exceptions.RokuUnreachableException">The device did not respond.</exception>
    Task<RokuDeviceInfo> GetDeviceInfoAsync(string host, int port, CancellationToken ct = default);

    /// <summary>
    /// Sends <c>/keypress</c>, <c>/keydown</c> or <c>/keyup</c>.
    /// </summary>
    /// <param name="host">IP or host name.</param>
    /// <param name="port">ECP port.</param>
    /// <param name="key">The key.</param>
    /// <param name="action">Press, down or up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    /// <exception cref="Exceptions.RokuUnreachableException">The device did not respond.</exception>
    Task SendKeyAsync(string host, int port, RokuKey key, KeyAction action, CancellationToken ct = default);

    /// <summary>
    /// Types one character via <c>/keypress/Lit_{char}</c>.
    /// </summary>
    /// <param name="host">IP or host name.</param>
    /// <param name="port">ECP port.</param>
    /// <param name="character">One Unicode scalar value, as a string.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    /// <exception cref="Exceptions.RokuUnreachableException">The device did not respond.</exception>
    Task SendLiteralAsync(string host, int port, string character, CancellationToken ct = default);

    /// <summary>
    /// Lists installed channels (<c>/query/apps</c>).
    /// </summary>
    /// <param name="host">IP or host name.</param>
    /// <param name="port">ECP port.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The channels.</returns>
    /// <exception cref="Exceptions.RokuUnreachableException">The device did not respond.</exception>
    Task<IReadOnlyList<RokuAppDto>> GetAppsAsync(string host, int port, CancellationToken ct = default);

    /// <summary>
    /// Launches a channel (<c>/launch/{appId}</c>).
    /// </summary>
    /// <param name="host">IP or host name.</param>
    /// <param name="port">ECP port.</param>
    /// <param name="appId">Channel id; <c>dev</c> for the sideloaded channel.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    /// <exception cref="Exceptions.RokuUnreachableException">The device did not respond or refused.</exception>
    Task LaunchAsync(string host, int port, string appId, CancellationToken ct = default);
}
