using SixBench.Services.Capture;

namespace SixBench.Tests.Capture;

public class AudioPairingHeuristicTests
{
    private static AudioDeviceInfo A(string name) => new(name, name);

    [Fact]
    public void Pairs_by_shared_name()
    {
        var audio = new[] { A("MacBook Pro Microphone"), A("Cam Link 4K"), A("Elgato HD60") };

        Assert.Equal("Cam Link 4K", AudioPairingHeuristic.Pair("Cam Link 4K", audio)?.Name);
    }

    [Fact]
    public void Pairs_generic_usb_dongle_with_the_only_usb_audio_device()
    {
        var audio = new[] { A("MacBook Pro Microphone"), A("USB Digital Audio") };

        Assert.Equal("USB Digital Audio", AudioPairingHeuristic.Pair("USB Video", audio)?.Name);
    }

    [Fact]
    public void Never_pairs_built_in_microphones()
    {
        var audio = new[] { A("MacBook Pro Microphone"), A("Microsoft Teams Audio") };

        Assert.Null(AudioPairingHeuristic.Pair("FaceTime HD Camera", audio));
    }

    [Fact]
    public void Ambiguous_usb_audio_is_not_guessed()
    {
        var audio = new[] { A("USB Audio Device"), A("USB Digital Audio") };

        Assert.Null(AudioPairingHeuristic.Pair("USB Video", audio));
    }
}
