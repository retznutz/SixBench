using SixBench.Services.Streaming;

namespace SixBench.Tests.Streaming;

public class OggOpusParserTests
{
    private static List<byte[]> ParseAll(IEnumerable<ReadOnlyMemory<byte>> chunks)
    {
        var parser = new OggOpusParser();
        return chunks.SelectMany(c => parser.Push(c.Span)).ToList();
    }

    [Fact]
    public void Extracts_opus_packets_and_skips_headers()
    {
        var data = Fixture.Read("sine-1s.opus.ogg");

        var packets = ParseAll([data]);

        // One second of 20 ms packets (ffprobe reports 51 including encoder pre-skip).
        Assert.InRange(packets.Count, 50, 52);
        Assert.DoesNotContain(packets, p => p.AsSpan().StartsWith("OpusHead"u8) || p.AsSpan().StartsWith("OpusTags"u8));

        // TOC byte: config 0-31 with frame size code 3 = 20 ms for CELT/hybrid low-delay modes.
        Assert.All(packets, p => Assert.Equal(3, (p[0] >> 3) & 0x3));
    }

    [Theory]
    [InlineData(11)]
    [InlineData(12)]
    public void Chunked_input_yields_identical_packets(int seed)
    {
        var data = Fixture.Read("sine-1s.opus.ogg");

        Assert.Equal(ParseAll([data]), ParseAll(Fixture.Chunks(data, seed, maxChunk: 64)));
    }

    [Fact]
    public void Resynchronises_after_garbage()
    {
        var data = Fixture.Read("sine-1s.opus.ogg");
        byte[] corrupted = [1, 2, 3, 4, 5, .. data];

        Assert.Equal(ParseAll([data]).Count, ParseAll([corrupted]).Count);
    }
}
