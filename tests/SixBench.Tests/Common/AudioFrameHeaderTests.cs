using SixBench.Common.Streaming;

namespace SixBench.Tests.Common;

public class AudioFrameHeaderTests
{
    [Fact]
    public void Round_trips()
    {
        var header = new AudioFrameHeader(AudioFrameFlags.Discontinuity, 0xFFFFFFFE, 123456);

        var framed = header.Frame([9, 8, 7]);

        Assert.Equal(AudioFrameHeader.Size + 3, framed.Length);
        Assert.True(AudioFrameHeader.TryRead(framed, out var read));
        Assert.Equal(header, read);
        Assert.Equal([9, 8, 7], framed[AudioFrameHeader.Size..]);
    }

    [Fact]
    public void Uses_big_endian_layout_shared_with_the_browser()
    {
        // Same bytes as web/tests/audioFrame.test.ts.
        byte[] bytes = [0x56, 0x41, 0x01, 0x00, 0x00, 0x00, 0x01, 0x02, 0x00, 0x00, 0x03, 0xE8];

        Assert.True(AudioFrameHeader.TryRead(bytes, out var header));
        Assert.Equal(new AudioFrameHeader(AudioFrameFlags.Mic, 258, 1000), header);
    }

    [Fact]
    public void Annex_b_video_is_never_mistaken_for_audio()
    {
        Assert.False(AudioFrameHeader.HasMagic([0, 0, 0, 1, 0x09, 0xF0, 0, 0, 0, 1, 0x65, 0x88]));
        Assert.False(AudioFrameHeader.TryRead([0x56, 0x41, 0], out _));
    }
}
