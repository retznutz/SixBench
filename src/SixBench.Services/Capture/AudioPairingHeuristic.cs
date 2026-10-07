namespace SixBench.Services.Capture;

/// <summary>
/// Guesses which audio device belongs to a capture encoder's video device.
/// HDMI capture dongles expose video and audio as separate OS devices with related names
/// (e.g. "USB Video" + "USB Digital Audio"). The user can override the guess on the encoder link.
/// </summary>
public static class AudioPairingHeuristic
{
    private static readonly HashSet<string> GenericTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "usb", "video", "audio", "capture", "device", "digital", "interface", "camera", "hd", "input",
        "microphone", "mic", "stream", "0", "1", "2", "3", "3.0", "2.0",
    };

    private static readonly string[] BuiltInMarkers =
    [
        "macbook", "iphone", "ipad", "teams", "zoom", "blackhole", "soundflower", "loopback",
        "built-in", "realtek", "stereo mix", "array",
    ];

    /// <summary>
    /// Picks the best matching audio device for <paramref name="videoName"/>, or null when none is convincing.
    /// </summary>
    /// <param name="videoName">The video device name.</param>
    /// <param name="audioDevices">Available audio devices.</param>
    /// <returns>The paired audio device, or null.</returns>
    public static AudioDeviceInfo? Pair(string videoName, IReadOnlyList<AudioDeviceInfo> audioDevices)
    {
        if (audioDevices.Count == 0)
        {
            return null;
        }

        var videoTokens = Tokenize(videoName);
        AudioDeviceInfo? best = null;
        var bestScore = 0;

        foreach (var audio in audioDevices)
        {
            if (IsBuiltIn(audio.Name))
            {
                continue;
            }

            var score = Tokenize(audio.Name).Intersect(videoTokens, StringComparer.OrdinalIgnoreCase).Count() * 10;
            if (audio.Name.Contains(videoName, StringComparison.OrdinalIgnoreCase)
                || videoName.Contains(audio.Name, StringComparison.OrdinalIgnoreCase))
            {
                score += 20;
            }

            if (score > bestScore)
            {
                best = audio;
                bestScore = score;
            }
        }

        if (best is not null)
        {
            return best;
        }

        // Generic dongles: a USB video device and exactly one non-built-in USB audio device.
        if (videoName.Contains("usb", StringComparison.OrdinalIgnoreCase))
        {
            var usbAudio = audioDevices
                .Where(a => !IsBuiltIn(a.Name) && a.Name.Contains("usb", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (usbAudio.Count == 1)
            {
                return usbAudio[0];
            }
        }

        return null;
    }

    private static bool IsBuiltIn(string name) =>
        BuiltInMarkers.Any(m => name.Contains(m, StringComparison.OrdinalIgnoreCase));

    private static HashSet<string> Tokenize(string name) =>
        name.Split([' ', '-', '_', '(', ')', '[', ']', ':', ',', '/'], StringSplitOptions.RemoveEmptyEntries)
            .Where(t => !GenericTokens.Contains(t))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
