using System.Text.Json;
using SixBench.Common.Streaming;
using SixBench.Services.Capture;

namespace SixBench.Services.Streaming;

/// <summary>
/// JSON settings for stream-socket control messages (snake_case, e.g. <c>audio_grant</c>).
/// </summary>
public static class StreamJson
{
    /// <summary>Serializer options for control messages.</summary>
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };
}

/// <summary>Server → client: session established.</summary>
/// <param name="SessionId">This client's id.</param>
/// <param name="StableId">Encoder being streamed.</param>
/// <param name="Name">Encoder display name.</param>
/// <param name="AudioGrant">What audio the server permits.</param>
public sealed record HelloMessage(string SessionId, string StableId, string Name, AudioGrant AudioGrant)
{
    /// <summary>Message type.</summary>
    public string Type => StreamProtocol.Hello;
}

/// <summary>Server → client: device audio was turned on or off.</summary>
/// <param name="Enabled">Whether device audio is now flowing to this client.</param>
/// <param name="Error">Error code when audio stopped unexpectedly.</param>
/// <param name="Message">Human-readable detail.</param>
public sealed record AudioStateMessage(bool Enabled, string? Error = null, string? Message = null)
{
    /// <summary>Message type.</summary>
    public string Type => StreamProtocol.AudioState;
}

/// <summary>Server → client: response to <c>mic_claim</c>.</summary>
/// <param name="Granted">Whether the client now holds the microphone.</param>
/// <param name="Reason">Error code when not granted.</param>
public sealed record MicClaimResultMessage(bool Granted, string? Reason)
{
    /// <summary>Message type.</summary>
    public string Type => StreamProtocol.MicClaimResult;
}

/// <summary>Server → client: an error.</summary>
/// <param name="Code">Error code from <see cref="StreamProtocol.ErrorCodes"/>.</param>
/// <param name="Message">Human-readable detail.</param>
public sealed record ErrorMessage(string Code, string Message)
{
    /// <summary>Message type.</summary>
    public string Type => StreamProtocol.Error;
}

/// <summary>Server → client: the capture pipeline ended and the socket will close.</summary>
/// <param name="Reason">Why: <c>capture_failed</c>, <c>settings_changed</c>, <c>shutdown</c>.</param>
/// <param name="Message">Human-readable detail (e.g. ffmpeg's last error lines).</param>
public sealed record StreamEndedMessage(string Reason, string? Message)
{
    /// <summary>Message type.</summary>
    public string Type => StreamProtocol.StreamEnded;
}
