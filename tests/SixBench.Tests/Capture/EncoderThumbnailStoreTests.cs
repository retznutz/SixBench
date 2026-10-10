using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SixBench.Common.Options;
using SixBench.Services.Capture;
using SixBench.Services.Exceptions;

namespace SixBench.Tests.Capture;

public sealed class EncoderThumbnailStoreTests : IDisposable
{
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46];

    // A DirectShow moniker: too long and too awkward to use as a file name.
    private const string StableId = @"@device_pnp_\\?\usb#vid_534d&pid_2109&mi_00#7&2f1b1e2b&0&0000#{65e8773d-8f56-11d0-a3b9-00a0c9223196}\global";

    private readonly string _root = Directory.CreateTempSubdirectory("sixbench-thumbs-").FullName;

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private EncoderThumbnailStore CreateStore(int maxKilobytes = 1024) => new(
        Options.Create(new ThumbnailOptions { Directory = "thumbs", MaxKilobytes = maxKilobytes }),
        new FakeEnvironment(_root));

    [Fact]
    public void Missing_thumbnail_is_null_and_creates_nothing()
    {
        Assert.Null(CreateStore().Find(StableId));
        Assert.False(Directory.Exists(Path.Combine(_root, "thumbs")));
    }

    [Fact]
    public async Task Saves_under_a_hashed_name_relative_to_the_content_root()
    {
        var store = CreateStore();

        await store.SaveAsync(StableId, Jpeg);

        var thumbnail = Assert.IsType<EncoderThumbnail>(store.Find(StableId));
        Assert.Equal(EncoderThumbnailStore.FileName(StableId), Path.GetFileName(thumbnail.Path));
        Assert.Matches("^[0-9a-f]{64}\\.jpg$", Path.GetFileName(thumbnail.Path));
        Assert.StartsWith(_root.Replace('\\', '/'), thumbnail.Path);
        Assert.DoesNotContain('\\', thumbnail.Path);
        Assert.Equal(Jpeg, await File.ReadAllBytesAsync(thumbnail.Path));
        Assert.Single(Directory.GetFiles(Path.Combine(_root, "thumbs")));
    }

    [Fact]
    public async Task Replaces_an_existing_thumbnail()
    {
        var store = CreateStore();
        await store.SaveAsync(StableId, Jpeg);

        byte[] newer = [.. Jpeg, 0x01, 0x02];
        await store.SaveAsync(StableId, newer);

        Assert.Equal(newer, await File.ReadAllBytesAsync(store.Find(StableId)!.Path));
        Assert.Single(Directory.GetFiles(Path.Combine(_root, "thumbs")));
    }

    [Fact]
    public async Task Rejects_empty_non_jpeg_and_oversized_images()
    {
        var store = CreateStore(maxKilobytes: 1);
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        byte[] tooBig = [.. Jpeg, .. new byte[1024]];

        await Assert.ThrowsAsync<ServiceValidationException>(() => store.SaveAsync(StableId, []));
        await Assert.ThrowsAsync<ServiceValidationException>(() => store.SaveAsync(StableId, png));
        await Assert.ThrowsAsync<ServiceValidationException>(() => store.SaveAsync(StableId, tooBig));
        Assert.Null(store.Find(StableId));
    }

    private sealed class FakeEnvironment(string contentRoot) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";

        public string ApplicationName { get; set; } = "SixBench.Tests";

        public string ContentRootPath { get; set; } = contentRoot;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
