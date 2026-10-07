namespace SixBench.Tests;

internal static class Fixture
{
    public static byte[] Read(string name) => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    /// <summary>Splits data into pseudo-random chunk sizes to exercise incremental parsers.</summary>
    public static IEnumerable<ReadOnlyMemory<byte>> Chunks(byte[] data, int seed, int maxChunk = 997)
    {
        var random = new Random(seed);
        var offset = 0;
        while (offset < data.Length)
        {
            var size = Math.Min(random.Next(1, maxChunk), data.Length - offset);
            yield return data.AsMemory(offset, size);
            offset += size;
        }
    }
}
