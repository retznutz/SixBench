using SixBench.Services.Capture;

namespace SixBench.Tests.Capture;

public class DeviceListParsersTests
{
    // Captured from ffmpeg 7.0.1 on macOS (with a capture dongle added).
    private const string AvFoundation = """
        2026-10-07 09:53:54.415 ffmpeg[90883:83582591] WARNING: Add NSCameraUseContinuityCameraDeviceType to your Info.plist.
        [AVFoundation indev @ 0x7fbeb672ff00] AVFoundation video devices:
        [AVFoundation indev @ 0x7fbeb672ff00] [0] FaceTime HD Camera
        [AVFoundation indev @ 0x7fbeb672ff00] [1] USB3.0 Capture
        [AVFoundation indev @ 0x7fbeb672ff00] [2] Capture screen 0
        [AVFoundation indev @ 0x7fbeb672ff00] AVFoundation audio devices:
        [AVFoundation indev @ 0x7fbeb672ff00] [0] MacBook Pro Microphone
        [AVFoundation indev @ 0x7fbeb672ff00] [1] USB3.0 Capture
        [in#0 @ 0x7fbeb672f5c0] Error opening input: Input/output error
        Error opening input file .
        """;

    [Fact]
    public void Parses_avfoundation_and_drops_screen_capture()
    {
        var (video, audio) = DeviceListParsers.ParseAvFoundation(AvFoundation);

        Assert.Equal(["FaceTime HD Camera", "USB3.0 Capture"], video);
        Assert.Equal(["MacBook Pro Microphone", "USB3.0 Capture"], audio);
    }

    [Fact]
    public void Parses_system_profiler_unique_ids()
    {
        const string json = """
            { "SPCameraDataType" : [ { "_name" : "FaceTime HD Camera", "spcamera_model-id" : "FaceTime HD Camera",
              "spcamera_unique-id" : "47B4B64B-7067-4B9C-AD2B-AE273A71F4B5" } ] }
            """;

        var ids = DeviceListParsers.ParseSystemProfilerCameras(json);

        Assert.Equal("47B4B64B-7067-4B9C-AD2B-AE273A71F4B5", ids["facetime hd camera"]);
        Assert.Empty(DeviceListParsers.ParseSystemProfilerCameras("not json"));
    }

    [Fact]
    public void Parses_dshow_inline_format_with_alternative_names()
    {
        const string stderr = """
            [dshow @ 000001c3a6f4e3c0] "USB Video" (video)
            [dshow @ 000001c3a6f4e3c0]   Alternative name "@device_pnp_\\?\usb#vid_534d&pid_2109&mi_00#7&1b7d1a9b&0&0000#{65e8773d-8f56-11d0-a3b9-00a0c9223196}\global"
            [dshow @ 000001c3a6f4e3c0] "OBS Virtual Camera" (none)
            [dshow @ 000001c3a6f4e3c0]   Alternative name "@device_sw_{860BB310-5D01-11D0-BD3B-00A0C911CE86}\{A3FCE0F5-3493-419F-958A-ABA1250EC20B}"
            [dshow @ 000001c3a6f4e3c0] "Digital Audio Interface (USB Digital Audio)" (audio)
            [dshow @ 000001c3a6f4e3c0]   Alternative name "@device_cm_{33D9A762-90C8-11D0-BD43-00A0C911CE86}\wave_{1F0B3C7A-0000-0000-0000-000000000000}"
            dummy: Immediate exit requested
            """;

        var (video, audio) = DeviceListParsers.ParseDirectShow(stderr);

        var v = Assert.Single(video);
        Assert.Equal("USB Video", v.Name);
        Assert.StartsWith("@device_pnp_", v.AlternativeName);
        var a = Assert.Single(audio);
        Assert.Equal("Digital Audio Interface (USB Digital Audio)", a.Name);
        Assert.StartsWith("@device_cm_", a.AlternativeName);
    }

    [Fact]
    public void Parses_dshow_section_format()
    {
        const string stderr = """
            [dshow @ 0000020] DirectShow video devices (some may be both video and audio devices)
            [dshow @ 0000020]  "USB Video"
            [dshow @ 0000020]     Alternative name "@device_pnp_\\?\usb#vid_534d"
            [dshow @ 0000020] DirectShow audio devices
            [dshow @ 0000020]  "USB Digital Audio"
            [dshow @ 0000020]     Alternative name "@device_cm_{33D9A762}\wave_{abc}"
            """;

        var (video, audio) = DeviceListParsers.ParseDirectShow(stderr);

        Assert.Equal("USB Video", Assert.Single(video).Name);
        Assert.Equal("USB Digital Audio", Assert.Single(audio).Name);
    }

    [Fact]
    public void Parses_arecord_capture_devices()
    {
        const string output = """
            **** List of CAPTURE Hardware Devices ****
            card 0: PCH [HDA Intel PCH], device 0: ALC3246 Analog [ALC3246 Analog]
              Subdevices: 1/1
            card 2: MS2109 [MS2109], device 0: USB Audio [USB Audio]
              Subdevices: 1/1
            """;

        var devices = DeviceListParsers.ParseArecord(output);

        Assert.Equal(
            [new AudioDeviceInfo("HDA Intel PCH", "hw:CARD=PCH,DEV=0"), new AudioDeviceInfo("MS2109", "hw:CARD=MS2109,DEV=0")],
            devices);
    }
}
