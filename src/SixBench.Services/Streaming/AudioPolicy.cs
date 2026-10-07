using Microsoft.Extensions.Options;
using SixBench.Common.Options;
using SixBench.Data.Entities;
using SixBench.Services.Capture;

namespace SixBench.Services.Streaming;

/// <summary>
/// Decides what audio a session may use.
/// </summary>
public interface IAudioPolicy
{
    /// <summary>
    /// Computes the audio grant for an encoder.
    /// </summary>
    /// <param name="link">The encoder link, if any.</param>
    /// <param name="audioInput">The resolved audio input, if any.</param>
    /// <returns>The grant sent to clients.</returns>
    AudioGrant Evaluate(EncoderLink? link, string? audioInput);
}

/// <summary>
/// Device audio requires the global switch, the link's flag and an audio input.
/// The microphone uplink is never granted: Roku has no documented way to accept audio input.
/// </summary>
/// <param name="options">Audio options.</param>
public sealed class AudioPolicy(IOptionsMonitor<AudioOptions> options) : IAudioPolicy
{
    /// <inheritdoc />
    public AudioGrant Evaluate(EncoderLink? link, string? audioInput) => new(
        Device: options.CurrentValue.AllowDeviceAudio
            && link is { AllowDeviceAudio: true }
            && !string.IsNullOrWhiteSpace(audioInput),
        Mic: false);
}
