using SixBench.Services.Streaming;

namespace SixBench.Tests.Streaming;

public class AnnexBParserTests
{
    private static List<AccessUnit> ParseAll(byte[] data, IEnumerable<ReadOnlyMemory<byte>> chunks)
    {
        var parser = new AnnexBParser();
        var units = new List<AccessUnit>();
        foreach (var chunk in chunks)
        {
            units.AddRange(parser.Push(chunk.Span));
        }

        units.AddRange(parser.Flush());
        return units;
    }

    private static List<int> NalTypes(byte[] unit)
    {
        var types = new List<int>();
        for (var i = 0; i + 3 < unit.Length; i++)
        {
            if (unit[i] == 0 && unit[i + 1] == 0 && unit[i + 2] == 1)
            {
                types.Add(unit[i + 3] & 0x1F);
            }
        }

        return types;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Splits_ffmpeg_output_into_one_unit_per_frame_regardless_of_chunking(int seed)
    {
        var data = Fixture.Read("testsrc-30f-gop10.h264");

        var units = ParseAll(data, Fixture.Chunks(data, seed));

        Assert.Equal(30, units.Count);
        Assert.Equal([0, 10, 20], units.Select((u, i) => (u, i)).Where(x => x.u.IsKeyframe).Select(x => x.i));
        Assert.All(units, u => Assert.Equal(9, NalTypes(u.Data)[0]));
        Assert.All(units.Where(u => u.IsKeyframe), u => Assert.Contains(7, NalTypes(u.Data)));
        Assert.Equal(data.Length, units.Sum(u => u.Data.Length));
    }

    [Fact]
    public void Single_push_matches_byte_at_a_time()
    {
        var data = Fixture.Read("testsrc-30f-gop10.h264");

        var whole = ParseAll(data, [data]);
        var bytewise = ParseAll(data, Fixture.Chunks(data, 7, maxChunk: 2));

        Assert.Equal(whole.Select(u => u.Data), bytewise.Select(u => u.Data));
    }

    [Fact]
    public void Inserts_cached_sps_and_pps_before_an_idr_that_lacks_them()
    {
        byte[] sps = [0, 0, 0, 1, 0x67, 0x42, 0xC0, 0x1E, 0xAA];
        byte[] pps = [0, 0, 0, 1, 0x68, 0xCE, 0x3C, 0x80];
        byte[] aud = [0, 0, 0, 1, 0x09, 0xF0];
        byte[] idr = [0, 0, 0, 1, 0x65, 0x88, 0x84, 0x21];
        byte[] slice = [0, 0, 0, 1, 0x41, 0x9A, 0x02, 0x03];
        byte[] stream = [.. aud, .. sps, .. pps, .. idr, .. aud, .. slice, .. aud, .. idr, .. aud, .. slice];

        var units = ParseAll(stream, [stream]);

        Assert.Equal(4, units.Count);
        Assert.Equal([9, 7, 8, 5], NalTypes(units[2].Data));
        Assert.True(units[2].IsKeyframe);
        Assert.Equal([9, 1], NalTypes(units[3].Data));
    }

    [Fact]
    public void Splits_on_first_slice_when_there_are_no_access_unit_delimiters()
    {
        byte[] first = [0, 0, 0, 1, 0x65, 0x88, 0x84];
        byte[] secondSliceOfSameFrame = [0, 0, 1, 0x65, 0x10, 0x84];
        byte[] nextFrame = [0, 0, 0, 1, 0x41, 0x9A, 0x02];
        byte[] stream = [.. first, .. secondSliceOfSameFrame, .. nextFrame];

        var units = ParseAll(stream, [stream]);

        Assert.Equal(2, units.Count);
        Assert.Equal([5, 5], NalTypes(units[0].Data));
        Assert.False(units[1].IsKeyframe);
    }
}
