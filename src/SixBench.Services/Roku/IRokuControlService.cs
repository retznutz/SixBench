using SixBench.Common.Enums;

namespace SixBench.Services.Roku;

/// <summary>
/// Sends remote-control input to a saved Roku.
/// </summary>
public interface IRokuControlService
{
    /// <summary>
    /// Sends a key.
    /// </summary>
    /// <param name="rokuId">Saved device id.</param>
    /// <param name="key">The key.</param>
    /// <param name="action">Press, down or up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    /// <exception cref="Exceptions.NotFoundException">No such device.</exception>
    /// <exception cref="Exceptions.RokuUnreachableException">The device did not respond.</exception>
    Task SendKeyAsync(int rokuId, RokuKey key, KeyAction action, CancellationToken ct = default);

    /// <summary>
    /// Types text into the on-screen keyboard, one character at a time.
    /// </summary>
    /// <param name="rokuId">Saved device id.</param>
    /// <param name="text">The text.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    /// <exception cref="Exceptions.NotFoundException">No such device.</exception>
    /// <exception cref="Exceptions.RokuUnreachableException">The device did not respond.</exception>
    Task SendTextAsync(int rokuId, string text, CancellationToken ct = default);
}
