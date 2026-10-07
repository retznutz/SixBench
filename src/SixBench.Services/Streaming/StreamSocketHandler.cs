using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixBench.Common.Options;
using SixBench.Common.Streaming;
using SixBench.Services.Exceptions;

namespace SixBench.Services.Streaming;

/// <summary>
/// Runs the stream WebSocket protocol for one browser connection.
/// </summary>
public interface IStreamSocketHandler
{
    /// <summary>
    /// Streams the device to the socket until either side closes.
    /// </summary>
    /// <param name="socket">An accepted WebSocket.</param>
    /// <param name="stableId">Capture device stable id.</param>
    /// <param name="ct">Request-aborted token.</param>
    /// <returns>A task that completes when the connection is finished.</returns>
    Task HandleAsync(WebSocket socket, string stableId, CancellationToken ct);
}

/// <summary>
/// Default <see cref="IStreamSocketHandler"/>.
/// Binary frames to the client are H.264 access units or <c>VA</c>-prefixed Opus packets;
/// text frames are JSON control messages (see <see cref="StreamProtocol"/>).
/// </summary>
/// <param name="manager">Session manager.</param>
/// <param name="options">Streaming options.</param>
/// <param name="logger">Logger.</param>
public sealed class StreamSocketHandler(
    ICaptureSessionManager manager,
    IOptions<StreamingOptions> options,
    ILogger<StreamSocketHandler> logger) : IStreamSocketHandler
{
    private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(2);

    /// <inheritdoc />
    public async Task HandleAsync(WebSocket socket, string stableId, CancellationToken ct)
    {
        CaptureLease lease;
        try
        {
            lease = await manager.AcquireAsync(stableId, ct);
        }
        catch (Exception ex) when (ex is NotFoundException or InvalidOperationException or PlatformNotSupportedException)
        {
            logger.LogWarning("Stream request for {StableId} rejected: {Message}", stableId, ex.Message);
            var code = ex is NotFoundException ? "not_found" : StreamProtocol.ErrorCodes.CaptureFailed;
            using (var errorLock = new SemaphoreSlim(1, 1))
            {
                await SendTextAsync(socket, errorLock, Serialize(new ErrorMessage(code, ex.Message)), ct);
            }

            await CloseQuietlyAsync(socket, WebSocketCloseStatus.PolicyViolation, code);
            return;
        }

        var session = lease.Session;
        var subscriber = lease.Subscriber;
        logger.LogInformation("Client {ClientId} connected to {Name}", subscriber.Id, session.Source.Name);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        using var sendLock = new SemaphoreSlim(1, 1);
        try
        {
            subscriber.SendControl(new HelloMessage(subscriber.Id, session.Source.StableId, session.Source.Name, session.Source.AudioGrant));

            var mediaLoop = MediaLoopAsync(socket, subscriber, sendLock, cts.Token);
            var controlLoop = ControlLoopAsync(socket, subscriber, sendLock, cts.Token);
            var receiveLoop = ReceiveLoopAsync(socket, session, subscriber, cts.Token);

            await Task.WhenAny(receiveLoop, Task.WhenAll(mediaLoop, controlLoop));

            if (!receiveLoop.IsCompleted)
            {
                // The session ended: say goodbye, then give the client a moment to acknowledge.
                await CloseQuietlyAsync(socket, WebSocketCloseStatus.NormalClosure, "stream ended", sendLock);
                await Task.WhenAny(receiveLoop, Task.Delay(CloseTimeout, CancellationToken.None));
            }

            await cts.CancelAsync();
            await Task.WhenAll(Swallow(mediaLoop), Swallow(controlLoop), Swallow(receiveLoop));
        }
        finally
        {
            await manager.ReleaseAsync(lease);
            logger.LogInformation("Client {ClientId} disconnected from {Name}", subscriber.Id, session.Source.Name);
        }

        if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            await CloseQuietlyAsync(socket, WebSocketCloseStatus.NormalClosure, "bye");
        }
    }

    private static async Task MediaLoopAsync(WebSocket socket, StreamSubscriber subscriber, SemaphoreSlim sendLock, CancellationToken ct)
    {
        await foreach (var message in subscriber.Media.ReadAllAsync(ct))
        {
            await sendLock.WaitAsync(ct);
            try
            {
                await socket.SendAsync(message, WebSocketMessageType.Binary, endOfMessage: true, ct);
            }
            finally
            {
                sendLock.Release();
            }
        }
    }

    private static async Task ControlLoopAsync(WebSocket socket, StreamSubscriber subscriber, SemaphoreSlim sendLock, CancellationToken ct)
    {
        await foreach (var json in subscriber.Control.ReadAllAsync(ct))
        {
            await SendTextAsync(socket, sendLock, json, ct);
        }
    }

    private async Task ReceiveLoopAsync(WebSocket socket, CaptureSession session, StreamSubscriber subscriber, CancellationToken ct)
    {
        var maxBytes = options.Value.MaxControlMessageBytes;
        var buffer = ArrayPool<byte>.Shared.Rent(maxBytes);
        var warnedAboutBinary = false;
        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var length = 0;
                var oversized = false;
                WebSocketReceiveResult result;
                do
                {
                    var space = length < maxBytes ? new ArraySegment<byte>(buffer, length, maxBytes - length) : new ArraySegment<byte>(buffer, 0, maxBytes);
                    result = await socket.ReceiveAsync(space, ct);
                    if (length < maxBytes)
                    {
                        length += result.Count;
                    }
                    else
                    {
                        oversized = true;
                    }
                }
                while (!result.EndOfMessage && result.MessageType != WebSocketMessageType.Close);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return;
                }

                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    // Upstream audio (microphone) is not permitted in v1.
                    if (!warnedAboutBinary)
                    {
                        warnedAboutBinary = true;
                        subscriber.SendControl(new ErrorMessage(StreamProtocol.ErrorCodes.NotPermitted, "Upstream audio is not permitted."));
                    }

                    continue;
                }

                if (oversized || length >= maxBytes)
                {
                    subscriber.SendControl(new ErrorMessage(StreamProtocol.ErrorCodes.BadMessage, "Control message too large."));
                    continue;
                }

                HandleControlMessage(session, subscriber, buffer.AsSpan(0, length));
            }
        }
        catch (WebSocketException ex)
        {
            logger.LogDebug(ex, "Client {ClientId} socket error", subscriber.Id);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Applies one JSON control message from the client.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="subscriber">The client.</param>
    /// <param name="utf8Json">The message.</param>
    public static void HandleControlMessage(CaptureSession session, StreamSubscriber subscriber, ReadOnlySpan<byte> utf8Json)
    {
        string? type;
        JsonElement root;
        try
        {
            using var doc = JsonDocument.Parse(utf8Json.ToArray());
            root = doc.RootElement.Clone();
            type = root.TryGetProperty("type", out var t) ? t.GetString() : null;
        }
        catch (JsonException)
        {
            subscriber.SendControl(new ErrorMessage(StreamProtocol.ErrorCodes.BadMessage, "Invalid JSON."));
            return;
        }

        switch (type)
        {
            case StreamProtocol.AudioOn:
                switch (session.SetAudio(subscriber, true))
                {
                    case AudioToggleResult.Ok:
                        subscriber.SendControl(new AudioStateMessage(true));
                        break;
                    case AudioToggleResult.NotPermitted:
                        subscriber.SendControl(new ErrorMessage(
                            StreamProtocol.ErrorCodes.NotPermitted,
                            "Device audio is not permitted for this encoder."));
                        break;
                }

                break;

            case StreamProtocol.AudioOff:
                session.SetAudio(subscriber, false);
                subscriber.SendControl(new AudioStateMessage(false));
                break;

            case StreamProtocol.MicClaim:
                subscriber.SendControl(new MicClaimResultMessage(false, StreamProtocol.ErrorCodes.NotPermitted));
                break;

            case StreamProtocol.MicRelease:
                break;

            case StreamProtocol.Keyframe:
                session.RequestKeyframe(subscriber);
                break;

            case StreamProtocol.Stats:
                subscriber.UpdateStats(
                    GetString(root, "decoder"),
                    GetDouble(root, "fps"),
                    GetDouble(root, "jitter_ms"),
                    (long?)GetDouble(root, "dropped_frames"));
                break;

            default:
                subscriber.SendControl(new ErrorMessage(StreamProtocol.ErrorCodes.BadMessage, $"Unknown message type '{type}'."));
                break;
        }
    }

    private static string? GetString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static double? GetDouble(JsonElement root, string name) =>
        root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null;

    private static string Serialize<T>(T message) => JsonSerializer.Serialize(message, StreamJson.Options);

    private static async Task SendTextAsync(WebSocket socket, SemaphoreSlim sendLock, string json, CancellationToken ct)
    {
        await sendLock.WaitAsync(ct);
        try
        {
            if (socket.State == WebSocketState.Open)
            {
                await socket.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, endOfMessage: true, ct);
            }
        }
        finally
        {
            sendLock.Release();
        }
    }

    private static async Task CloseQuietlyAsync(
        WebSocket socket,
        WebSocketCloseStatus status,
        string description,
        SemaphoreSlim? sendLock = null)
    {
        using var timeout = new CancellationTokenSource(CloseTimeout);
        var locked = false;
        try
        {
            if (sendLock is not null)
            {
                await sendLock.WaitAsync(timeout.Token);
                locked = true;
            }

            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                await socket.CloseOutputAsync(status, description, timeout.Token);
            }
        }
        catch (Exception ex) when (ex is WebSocketException or OperationCanceledException or ObjectDisposedException)
        {
            // The client went away first.
        }
        finally
        {
            if (locked)
            {
                sendLock!.Release();
            }
        }
    }

    private static async Task Swallow(Task task)
    {
        try
        {
            await task;
        }
        catch (Exception ex) when (ex is OperationCanceledException or WebSocketException or ObjectDisposedException)
        {
            // Expected during teardown.
        }
    }
}
