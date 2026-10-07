namespace SixBench.Common.Streaming;

/// <summary>
/// Constants for the stream WebSocket protocol.
/// </summary>
/// <remarks>
/// Text frames carry JSON control messages with a <c>type</c> discriminator.
/// Binary frames from the server are either raw H.264 Annex-B access units (video)
/// or Opus packets prefixed with a 12-byte <see cref="AudioFrameHeader"/> (audio).
/// </remarks>
public static class StreamProtocol
{
    /// <summary>Server → client: session established; carries the audio grant.</summary>
    public const string Hello = "hello";
    /// <summary>Client → server: start receiving device audio.</summary>
    public const string AudioOn = "audio_on";
    /// <summary>Client → server: stop receiving device audio.</summary>
    public const string AudioOff = "audio_off";
    /// <summary>Server → client: device-audio state changed.</summary>
    public const string AudioState = "audio_state";
    /// <summary>Client → server: request the microphone uplink.</summary>
    public const string MicClaim = "mic_claim";
    /// <summary>Server → client: response to <see cref="MicClaim"/>.</summary>
    public const string MicClaimResult = "mic_claim_result";
    /// <summary>Client → server: release the microphone uplink.</summary>
    public const string MicRelease = "mic_release";
    /// <summary>Client → server: resend the current GOP starting at a keyframe.</summary>
    public const string Keyframe = "keyframe";
    /// <summary>Client → server: playback statistics.</summary>
    public const string Stats = "stats";
    /// <summary>Server → client: an error occurred.</summary>
    public const string Error = "error";
    /// <summary>Server → client: the capture pipeline ended (device unplugged, ffmpeg crash).</summary>
    public const string StreamEnded = "stream_ended";

    /// <summary>Error codes sent in <see cref="Error"/> and <see cref="MicClaimResult"/> messages.</summary>
    public static class ErrorCodes
    {
        /// <summary>The server does not permit the requested feature for this session.</summary>
        public const string NotPermitted = "not_permitted";
        /// <summary>Another client already holds the microphone.</summary>
        public const string MicBusy = "mic_busy";
        /// <summary>Audio is not available for this device.</summary>
        public const string AudioUnavailable = "audio_unavailable";
        /// <summary>The control message could not be understood.</summary>
        public const string BadMessage = "bad_message";
        /// <summary>The capture pipeline failed.</summary>
        public const string CaptureFailed = "capture_failed";
    }
}
