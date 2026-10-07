using SixBench.Common.Dtos;

namespace SixBench.Services.Capture;

/// <summary>
/// Combines detected capture devices with saved encoder links.
/// </summary>
public interface ICaptureDeviceService
{
    /// <summary>
    /// Lists detected encoders merged with saved links. Saved links whose device is missing are included
    /// with <see cref="CaptureDeviceDto.IsConnected"/> = false.
    /// </summary>
    /// <param name="refresh">Re-enumerate instead of using the short cache.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All encoders.</returns>
    Task<IReadOnlyList<CaptureDeviceDto>> ListAsync(bool refresh = false, CancellationToken ct = default);

    /// <summary>
    /// Gets one encoder.
    /// </summary>
    /// <param name="stableId">Capture device stable id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The encoder.</returns>
    /// <exception cref="Exceptions.NotFoundException">No such device or link.</exception>
    Task<CaptureDeviceDto> GetAsync(string stableId, CancellationToken ct = default);

    /// <summary>
    /// Lists the host's audio capture devices (for choosing an encoder's audio input manually).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Audio device names and inputs.</returns>
    Task<IReadOnlyList<AudioInputDto>> ListAudioInputsAsync(CancellationToken ct = default);

    /// <summary>
    /// Resolves everything needed to start capturing from a connected encoder.
    /// </summary>
    /// <param name="stableId">Capture device stable id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The capture source.</returns>
    /// <exception cref="Exceptions.NotFoundException">The device is not connected.</exception>
    Task<CaptureSource> ResolveSourceAsync(string stableId, CancellationToken ct = default);
}
