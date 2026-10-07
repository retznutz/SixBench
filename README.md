# SixBench

Watch a physical Roku in the browser through an HDMI-to-USB capture encoder, and control it with an on-screen remote, keyboard shortcuts and text entry.

```
Roku ──HDMI──▶ USB capture encoder ──USB──▶ SixBench server (ffmpeg) ──WebSocket──▶ Browser (WebCodecs)
  ▲                                                                                     │
  └──────────────────────────── Roku ECP (HTTP :8060) ◀── remote buttons / keyboard ◀───┘
```

## Solution layout

| Project | Purpose |
|---|---|
| `src/SixBench.Common` | DTOs, enums (`RokuKey`), stream protocol constants, the `VA` audio frame header, options, `PathUtil`, `DeviceKey` |
| `src/SixBench.Data` | EF Core + SQLite: `RokuDevice` and `EncoderLink` tables (singular names), repositories, migrations |
| `src/SixBench.Services` | Capture-device enumeration (AVFoundation / DirectShow / V4L2), ffmpeg pipelines, H.264 access-unit splitting, session fan-out, the stream WebSocket handler, Roku ECP control and SSDP discovery |
| `src/SixBench.Api` | ASP.NET Core Web API: versioned controllers, Swagger, Serilog, Application Insights, health checks, static hosting of the SPA |
| `tests/SixBench.Tests` | xUnit tests (no hardware or network required) |
| `web/` | Nuxt 4 SPA (TypeScript, Pinia, PrimeVue 4, Tailwind CSS 4) |

Dependencies flow `Api → Services → Data → Common`.

## Prerequisites

- **.NET SDK 10** (`global.json` pins 10.0.x).
- **ffmpeg** with `libx264` and `libopus`, on `PATH` or set via `Ffmpeg:Path`. On macOS: `brew install ffmpeg`. On Linux, also install `alsa-utils` (for `arecord`).
- **Node.js ≥ 22.22.3** (24 LTS recommended; see `web/.nvmrc`). Nuxt 4.6 requires it.
- **macOS:** the app that runs the server (Terminal, VS Code, …) needs **Camera** and **Microphone** permission (System Settings › Privacy & Security). The first capture triggers the prompt.
- **Roku:** *Settings › System › Advanced system settings › Control by mobile apps* must allow network access, or ECP commands are rejected.

## Running in development

```bash
# API on http://localhost:5216 (Swagger at /swagger)
dotnet run --project src/SixBench.Api --launch-profile http

# Frontend on http://localhost:3000 (proxies /api to the API)
cd web
npm install
npm run dev
```

Open http://localhost:3000. Then:
1. Go to **Settings** and click **Discover** to find Rokus, or add one by IP address.
2. Click **Configure** on an encoder to link it to a Roku and choose its audio settings.
3. Go to **Encoders** and click **Watch**.

In development the browser opens the stream WebSocket directly to the API (`ws://localhost:5216`), because the Nuxt dev proxy does not forward WebSocket upgrades. Set `NUXT_API_DEV_TARGET` if the API runs elsewhere.

## Production build

```bash
dotnet publish src/SixBench.Api -c Release -o out
```

Publishing runs `npm ci && npm run generate` in `web/` and ships the static SPA as `wwwroot/`, so one process serves both the API and the UI. Pass `-p:SkipWeb=true` to skip the frontend build.

### HTTPS is required for remote viewers

WebCodecs only works in a **secure context**. `http://localhost` qualifies, but `http://192.168.x.x` from another machine does not. To watch from other devices on the LAN, serve HTTPS with a certificate those devices trust, e.g. via Kestrel configuration:

```json
"Kestrel": {
  "Endpoints": {
    "Https": { "Url": "https://0.0.0.0:7270", "Certificate": { "Path": "certs/sixbench.pfx", "Password": "…" } }
  }
}
```

## Configuration (`appsettings.json`)

| Section | Key | Default | Notes |
|---|---|---|---|
| `Kestrel` | `Endpoints:Http:Url` | `http://*:5216` | Listens on all interfaces. Change the port here; this overrides `launchSettings.json` and `ASPNETCORE_URLS`. If you change it, set `NUXT_API_DEV_TARGET` for `npm run dev`. |
| `ConnectionStrings` | `SixBench` | `Data Source=data/sixbench.db` | Relative paths resolve against the content root. Migrations run at startup. |
| `Ffmpeg` | `Path` | `ffmpeg` | Executable path. |
| | `VideoEncoder` | `libx264` | Also `h264_videotoolbox`, `h264_nvenc`, `h264_qsv`, `h264_vaapi`. |
| | `VideoBitrate` | `6M` | `null` uses the encoder default. |
| | `GopSeconds` | `1.0` | Keyframe interval, and the longest a new viewer waits for one. |
| | `DefaultFrameRate` / `DefaultVideoSize` / `DefaultPixelFormat` | `30` / – / – | Used when the encoder link has no override. |
| | `ExtraEncoderArgs` | – | Appended to the encoder arguments. |
| `Streaming` | `IdleShutdownSeconds` | `10` | ffmpeg stops this long after the last viewer leaves. |
| | `SubscriberQueueFrames` | `120` | Per-viewer queue; on overflow the server skips ahead to the next keyframe. |
| `Audio` | `AllowDeviceAudio` | `true` | Global switch; each encoder link must also allow it. |
| | `DeviceBitrate` | `128k` | Opus bitrate. |
| `Roku` | `DiscoveryTimeoutMs`, `EcpPort`, `RequestTimeoutMs`, `TextCharDelayMs` | `3000`, `8060`, `3000`, `25` | |
| `ApplicationInsights` | `ConnectionString` | empty | Telemetry is enabled only when set. Serilog events are forwarded to it. |
| `Serilog` | | console + rolling file `logs/` | Standard `Serilog.Settings.Configuration` format. |

If ffmpeg rejects a capture mode (for example *"Selected framerate is not supported"*), set **Frame rate / Video size / Pixel format** for that encoder under **Settings › Configure › Capture settings**.

## REST API (v1)

Full documentation is in Swagger (`/swagger`). Capture devices are addressed by `id`, a URL-safe (base64url) form of their stable OS identifier.

| Method | Route | |
|---|---|---|
| GET | `/api/v1/capture-devices[?refresh=true]` | Encoders merged with saved links |
| GET | `/api/v1/capture-devices/audio-inputs` | Audio devices for manual pairing |
| GET/PUT/DELETE | `/api/v1/encoder-links/{id}` | Link an encoder to a Roku and set its audio and capture settings |
| GET/POST | `/api/v1/roku-devices` | List, or add by IP address |
| POST | `/api/v1/roku-devices/discover` | SSDP discovery |
| POST | `/api/v1/roku-devices/{id}/keys` | `{ "key": "Home", "action": "Press" \| "Down" \| "Up" }` |
| POST | `/api/v1/roku-devices/{id}/text` | `{ "text": "…" }`, typed one character at a time |
| GET | `/api/v1/streams` | Active sessions and per-viewer stats |
| GET | `/health` | Health check |

## Stream WebSocket protocol

`GET /api/v1/streams/{id}/ws` (WebSocket; not shown in Swagger).

**Server → client, binary**
- **Video:** one raw H.264 Annex-B access unit per message: constrained baseline, AUD-delimited, SPS/PPS before every IDR. The browser derives the WebCodecs codec string (`avc1.PPCCLL`) from the SPS.
- **Audio:** `'V' 'A' | flags u8 | reserved u8 | seq u32 BE | timestamp-ms u32 BE` followed by one 20 ms Opus packet (48 kHz stereo). Flag bit 1 marks a discontinuity. Annex-B always starts with `00 00`, so any message without the `VA` magic is video.

**Server → client, JSON text**
- `hello {session_id, stable_id, name, audio_grant: {device, mic}}`
- `audio_state {enabled, error?, message?}`
- `mic_claim_result {granted, reason?}`
- `error {code, message}`, where `code` is one of `not_permitted | mic_busy | audio_unavailable | bad_message | capture_failed | not_found`
- `stream_ended {reason, message?}`, where `reason` is one of `capture_failed | settings_changed | idle | shutdown`

**Client → server, JSON text**
- `audio_on`, `audio_off`
- `keyframe` (the server resends the cached GOP)
- `stats {decoder, fps, jitter_ms, dropped_frames}`
- `mic_claim`, `mic_release`

New viewers and viewers recovering from a decode error get the cached current GOP, so playback starts on a keyframe right away.

## Browser support

| | Video | Device audio |
|---|---|---|
| Chrome / Edge 94+ | WebCodecs | WebCodecs `AudioDecoder` |
| Safari 16.4+ | WebCodecs | Only on versions that have `AudioDecoder`. Otherwise the page reports `audio:unavailable` and video continues. |
| Fallback (`?decoder=mse`) | JMuxer → MSE / ManagedMediaSource | Not available |

Keyboard shortcuts work while the video has focus (click it first):

| Key | Roku button |
|---|---|
| Arrow keys | D-pad |
| Enter | OK |
| Backspace / Esc | Back |
| H | Home |
| Space | Play/Pause |
| , and . | Rev / Fwd |
| R | Replay |
| I | Info (*) |
| + and − | Volume |
| T | Type text |

## Known limitations

- **No microphone to the Roku.** Roku ECP has no documented audio-input API and HDMI capture only carries signal one way, so the server always answers `mic_claim` with `not_permitted`. The protocol messages are in place for future work.
- **About one frame of server-side latency.** An access unit is only known to be complete when the next one starts.
- **No channel/app launcher in v1.**
- **PrimeVue stays on 4.x (MIT).** PrimeVue 5 moved to the PrimeUI license, which requires a license key.

## Tests and linting

```bash
dotnet test                 # xUnit: parsers, session fan-out, ECP client, repositories, API integration
cd web
npm run lint                # ESLint (+ Prettier config)
npm run format:check        # Prettier
npm run typecheck           # vue-tsc via Nuxt
npm test                    # Vitest: Annex-B / SPS codec string, audio frame header
```

## Branching

Gitflow: `main` holds releases, `develop` is the integration branch, and work happens on `feature/*` branches.
