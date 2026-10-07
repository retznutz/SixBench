using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging.Abstractions;
using SixBench.Common.Enums;
using SixBench.Common.Options;
using SixBench.Common.Streaming;
using SixBench.Services.Capture;
using SixBench.Services.Streaming;

namespace SixBench.Tests.Streaming;

public class CaptureSessionTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private static CaptureSource Source(bool deviceAudio = true) =>
        new("test", "Test", HostPlatform.MacOS, "cam", deviceAudio ? "mic" : null, 30, null, null, new AudioGrant(deviceAudio, false));

    private static AccessUnit Unit(int n, bool key) => new([0, 0, 0, 1, 0x09, (byte)n], key);

    private static async Task<byte[]> NextMedia(StreamSubscriber s)
    {
        using var cts = new CancellationTokenSource(Timeout);
        return await s.Media.ReadAsync(cts.Token);
    }

    private static async Task<JsonElement> NextControl(StreamSubscriber s)
    {
        using var cts = new CancellationTokenSource(Timeout);
        return JsonDocument.Parse(await s.Control.ReadAsync(cts.Token)).RootElement;
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + Timeout;
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "Timed out waiting for condition.");
            await Task.Delay(10);
        }
    }

    [Fact]
    public async Task Late_subscriber_receives_current_gop_starting_at_keyframe()
    {
        var factory = new FakeMediaFactory();
        await using var session = new CaptureSession(Source(), factory, new StreamingOptions(), NullLogger.Instance);
        session.Start();
        var early = session.Subscribe();

        foreach (var unit in new[] { Unit(1, true), Unit(2, false), Unit(3, true), Unit(4, false), Unit(5, false) })
        {
            await factory.Video.Writer.WriteAsync(unit);
        }

        for (var i = 1; i <= 5; i++)
        {
            Assert.Equal(i, (await NextMedia(early))[5]);
        }

        var late = session.Subscribe();
        var replayed = new List<int>();
        for (var i = 0; i < 3; i++)
        {
            replayed.Add((await NextMedia(late))[5]);
        }

        Assert.Equal([3, 4, 5], replayed);
    }

    [Fact]
    public async Task First_subscriber_skips_delta_frames_until_a_keyframe()
    {
        var factory = new FakeMediaFactory();
        await using var session = new CaptureSession(Source(), factory, new StreamingOptions(), NullLogger.Instance);
        session.Start();
        var sub = session.Subscribe();

        await factory.Video.Writer.WriteAsync(Unit(1, false));
        await factory.Video.Writer.WriteAsync(Unit(2, true));

        Assert.Equal(2, (await NextMedia(sub))[5]);
        Assert.Equal(1, sub.ServerDroppedFrames);
    }

    [Fact]
    public async Task Slow_subscriber_drops_queue_and_resumes_at_next_keyframe()
    {
        var factory = new FakeMediaFactory();
        await using var session = new CaptureSession(Source(), factory, new StreamingOptions { SubscriberQueueFrames = 8 }, NullLogger.Instance);
        session.Start();
        var sub = session.Subscribe();

        await factory.Video.Writer.WriteAsync(Unit(0, true));
        for (var i = 1; i <= 12; i++)
        {
            await factory.Video.Writer.WriteAsync(Unit(i, false));
        }

        await factory.Video.Writer.WriteAsync(Unit(50, true));
        await factory.Video.Writer.WriteAsync(Unit(51, false));
        await WaitUntil(() => session.GetSnapshot().FramesOut == 15);

        // Overflow at frame 8 discarded the queue; deltas were skipped until keyframe 50.
        Assert.Equal(50, (await NextMedia(sub))[5]);
        Assert.Equal(51, (await NextMedia(sub))[5]);
        Assert.True(sub.ServerDroppedFrames > 0);
    }

    [Fact]
    public async Task Keyframe_request_replays_gop()
    {
        var factory = new FakeMediaFactory();
        await using var session = new CaptureSession(Source(), factory, new StreamingOptions(), NullLogger.Instance);
        session.Start();
        var sub = session.Subscribe();
        await factory.Video.Writer.WriteAsync(Unit(1, true));
        await factory.Video.Writer.WriteAsync(Unit(2, false));
        await NextMedia(sub);
        await NextMedia(sub);

        session.RequestKeyframe(sub);

        Assert.Equal(1, (await NextMedia(sub))[5]);
        Assert.Equal(2, (await NextMedia(sub))[5]);
    }

    [Fact]
    public async Task Audio_is_framed_and_sent_only_to_subscribers_that_enabled_it()
    {
        var factory = new FakeMediaFactory();
        await using var session = new CaptureSession(Source(), factory, new StreamingOptions(), NullLogger.Instance);
        session.Start();
        var listener = session.Subscribe();
        var quiet = session.Subscribe();
        await factory.Video.Writer.WriteAsync(Unit(1, true));
        await NextMedia(listener);
        await NextMedia(quiet);

        Assert.Equal(AudioToggleResult.Ok, session.SetAudio(listener, true));
        Assert.Equal(1, factory.AudioSourcesCreated);
        await factory.Audio.Writer.WriteAsync([0xFC, 0x01]);

        var frame = await NextMedia(listener);
        Assert.True(AudioFrameHeader.TryRead(frame, out var header));
        Assert.Equal(AudioFrameFlags.Discontinuity, header.Flags);
        Assert.Equal([0xFC, 0x01], frame[AudioFrameHeader.Size..]);
        Assert.False(quiet.Media.TryRead(out _));

        session.SetAudio(listener, false);
        await WaitUntil(() => !session.GetSnapshot().AudioRunning);
    }

    [Fact]
    public async Task Audio_is_refused_without_grant()
    {
        var factory = new FakeMediaFactory();
        await using var session = new CaptureSession(Source(deviceAudio: false), factory, new StreamingOptions(), NullLogger.Instance);
        session.Start();
        var sub = session.Subscribe();

        Assert.Equal(AudioToggleResult.NotPermitted, session.SetAudio(sub, true));
        Assert.Equal(0, factory.AudioSourcesCreated);
    }

    [Fact]
    public async Task Pipeline_failure_ends_session_and_notifies_subscribers()
    {
        var factory = new FakeMediaFactory();
        await using var session = new CaptureSession(Source(), factory, new StreamingOptions(), NullLogger.Instance);
        var ended = new TaskCompletionSource();
        session.Ended += _ => ended.TrySetResult();
        session.Start();
        var sub = session.Subscribe();

        factory.FailureReason = "device unplugged";
        factory.Video.Writer.Complete();
        await ended.Task.WaitAsync(Timeout);

        var message = await NextControl(sub);
        Assert.Equal("stream_ended", message.GetProperty("type").GetString());
        Assert.Equal("capture_failed", message.GetProperty("reason").GetString());
        Assert.Equal("device unplugged", message.GetProperty("message").GetString());
        Assert.Throws<InvalidOperationException>(() => session.Subscribe());
    }

    [Fact]
    public async Task Mic_claim_is_always_refused()
    {
        var factory = new FakeMediaFactory();
        await using var session = new CaptureSession(Source(), factory, new StreamingOptions(), NullLogger.Instance);
        session.Start();
        var sub = session.Subscribe();

        StreamSocketHandler.HandleControlMessage(session, sub, """{"type":"mic_claim"}"""u8);

        var message = await NextControl(sub);
        Assert.Equal("mic_claim_result", message.GetProperty("type").GetString());
        Assert.False(message.GetProperty("granted").GetBoolean());
        Assert.Equal("not_permitted", message.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task Stats_and_bad_messages_are_handled()
    {
        var factory = new FakeMediaFactory();
        await using var session = new CaptureSession(Source(), factory, new StreamingOptions(), NullLogger.Instance);
        session.Start();
        var sub = session.Subscribe();

        StreamSocketHandler.HandleControlMessage(session, sub, """{"type":"stats","decoder":"webcodecs","fps":29.5,"jitter_ms":3.2,"dropped_frames":4}"""u8);
        StreamSocketHandler.HandleControlMessage(session, sub, "{nope"u8);

        Assert.Equal("webcodecs", sub.LastStats?.Decoder);
        Assert.Equal(29.5, sub.LastStats?.Fps);
        Assert.Equal(4, sub.LastStats?.DroppedFrames);
        Assert.Equal("bad_message", (await NextControl(sub)).GetProperty("code").GetString());
    }

    private sealed class FakeMediaFactory : IMediaSourceFactory
    {
        public Channel<AccessUnit> Video { get; } = Channel.CreateUnbounded<AccessUnit>();

        public Channel<byte[]> Audio { get; } = Channel.CreateUnbounded<byte[]>();

        public string? FailureReason { get; set; }

        public int AudioSourcesCreated { get; private set; }

        public IVideoSource CreateVideo(CaptureSource source) => new FakeVideo(this);

        public IAudioSource CreateAudio(CaptureSource source)
        {
            AudioSourcesCreated++;
            return new FakeAudio(Audio);
        }

        private sealed class FakeVideo(FakeMediaFactory owner) : IVideoSource
        {
            public string? FailureReason => owner.FailureReason;

            public async IAsyncEnumerable<AccessUnit> ReadAsync([EnumeratorCancellation] CancellationToken ct)
            {
                await foreach (var unit in owner.Video.Reader.ReadAllAsync(ct))
                {
                    yield return unit;
                }
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }

        private sealed class FakeAudio(Channel<byte[]> channel) : IAudioSource
        {
            public string? FailureReason => null;

            public IAsyncEnumerable<byte[]> ReadAsync(CancellationToken ct) => channel.Reader.ReadAllAsync(ct);

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
