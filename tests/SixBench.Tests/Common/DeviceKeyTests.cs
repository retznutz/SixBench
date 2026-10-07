using SixBench.Common.Utilities;

namespace SixBench.Tests.Common;

public class DeviceKeyTests
{
    [Theory]
    [InlineData("avf:47B4B64B-7067-4B9C-AD2B-AE273A71F4B5")]
    [InlineData(@"dshow:@device_pnp_\\?\usb#vid_534d&pid_2109&mi_00#7&1b7d1a9b&0&0000#{65e8773d-8f56-11d0-a3b9-00a0c9223196}\global")]
    [InlineData("v4l2:usb-MACROSILICON_USB_Video-video-index0")]
    public void Round_trips_to_url_safe_key(string stableId)
    {
        var key = DeviceKey.Encode(stableId);

        Assert.Matches("^[A-Za-z0-9_-]+$", key);
        Assert.True(DeviceKey.TryDecode(key, out var decoded));
        Assert.Equal(stableId, decoded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not base64!")]
    [InlineData(null)]
    public void Rejects_invalid_keys(string? key) => Assert.False(DeviceKey.TryDecode(key, out _));
}
