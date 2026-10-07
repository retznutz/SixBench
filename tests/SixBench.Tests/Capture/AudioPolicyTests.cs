using Microsoft.Extensions.Options;
using SixBench.Common.Options;
using SixBench.Data.Entities;
using SixBench.Services.Streaming;

namespace SixBench.Tests.Capture;

public class AudioPolicyTests
{
    private sealed class Monitor(AudioOptions value) : IOptionsMonitor<AudioOptions>
    {
        public AudioOptions CurrentValue => value;

        public AudioOptions Get(string? name) => value;

        public IDisposable? OnChange(Action<AudioOptions, string?> listener) => null;
    }

    [Theory]
    [InlineData(true, true, "mic", true)]
    [InlineData(false, true, "mic", false)]
    [InlineData(true, false, "mic", false)]
    [InlineData(true, true, null, false)]
    public void Device_audio_requires_config_link_and_input(bool global, bool linkAllows, string? input, bool expected)
    {
        var policy = new AudioPolicy(new Monitor(new AudioOptions { AllowDeviceAudio = global }));

        var grant = policy.Evaluate(new EncoderLink { AllowDeviceAudio = linkAllows }, input);

        Assert.Equal(expected, grant.Device);
        Assert.False(grant.Mic);
    }

    [Fact]
    public void Unlinked_encoders_get_no_audio() =>
        Assert.False(new AudioPolicy(new Monitor(new AudioOptions())).Evaluate(null, "mic").Device);
}
