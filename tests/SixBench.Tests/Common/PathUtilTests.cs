using SixBench.Common.Utilities;

namespace SixBench.Tests.Common;

public class PathUtilTests
{
    [Theory]
    [InlineData(@"C:\data\\sixbench.db", "C:/data/sixbench.db")]
    [InlineData("data//logs///x.log", "data/logs/x.log")]
    [InlineData(@"\\server\share\file", "//server/share/file")]
    [InlineData("  /usr/local/bin/ffmpeg ", "/usr/local/bin/ffmpeg")]
    [InlineData(null, "")]
    public void Normalize_uses_forward_slashes(string? input, string expected) =>
        Assert.Equal(expected, PathUtil.Normalize(input));

    [Fact]
    public void Combine_joins_with_forward_slashes() =>
        Assert.Equal("data/logs/app.log", PathUtil.Combine("data", @"logs\", "app.log"));

    [Fact]
    public void ResolveAgainst_makes_relative_paths_absolute()
    {
        var resolved = PathUtil.ResolveAgainst("data/sixbench.db", "/srv/app");

        Assert.EndsWith("/srv/app/data/sixbench.db", resolved);
        Assert.DoesNotContain('\\', resolved);
    }
}
