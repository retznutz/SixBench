namespace SixBench.Common.Dtos;

/// <summary>
/// Diagnostics for an active capture session.
/// </summary>
/// <param name="Id">URL-safe device key.</param>
/// <param name="StableId">The capture device being streamed.</param>
/// <param name="StartedUtc">When ffmpeg was started.</param>
/// <param name="ClientCount">Number of connected browsers.</param>
/// <param name="AudioClientCount">Number of browsers receiving device audio.</param>
/// <param name="FramesOut">Total video access units produced.</param>
/// <param name="BytesOut">Total video bytes produced.</param>
/// <param name="AudioRunning">Whether the device-audio pipeline is running.</param>
/// <param name="Clients">Latest stats reported by each client.</param>
public sealed record StreamSessionDto(
    string Id,
    string StableId,
    DateTime StartedUtc,
    int ClientCount,
    int AudioClientCount,
    long FramesOut,
    long BytesOut,
    bool AudioRunning,
    IReadOnlyList<ClientStatsDto> Clients);

/// <summary>
/// Playback statistics reported by a browser over the stream socket.
/// </summary>
/// <param name="ClientId">Server-assigned session id.</param>
/// <param name="Decoder">Decoder in use (<c>webcodecs</c> or <c>mse</c>).</param>
/// <param name="Fps">Rendered frames per second.</param>
/// <param name="JitterMs">Frame arrival jitter in milliseconds.</param>
/// <param name="DroppedFrames">Frames dropped by the client.</param>
/// <param name="ServerDroppedFrames">Frames dropped by the server for this client (slow consumer).</param>
/// <param name="ReportedUtc">When the stats were received.</param>
public sealed record ClientStatsDto(
    string ClientId,
    string? Decoder,
    double? Fps,
    double? JitterMs,
    long? DroppedFrames,
    long ServerDroppedFrames,
    DateTime? ReportedUtc);
