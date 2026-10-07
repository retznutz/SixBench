import { isAudioFrame, readAudioHeader } from './audioFrame'
import type { DeviceAudioPlayer } from './DeviceAudioPlayer'
import type { VideoRenderer } from './VideoRenderer'
import type {
  ClientMessage,
  HelloMessage,
  PlaybackStats,
  ServerMessage,
  StreamErrorCode,
  StreamStatus,
} from '../../types/stream-protocol'

const STATS_INTERVAL_MS = 2000
const MAX_BACKOFF_MS = 10000

export interface StreamClientEvents {
  onStatus: (status: StreamStatus, message?: string) => void
  onHello: (hello: HelloMessage) => void
  onAudioState: (enabled: boolean, error?: StreamErrorCode, message?: string) => void
  onServerError: (code: StreamErrorCode, message: string) => void
  onStats: (stats: PlaybackStats) => void
}

/**
 * Owns the stream WebSocket: routes binary frames to the video renderer or audio player,
 * reports stats, and reconnects with backoff.
 */
export class StreamClient {
  private ws: WebSocket | null = null
  private audio: DeviceAudioPlayer | null = null
  private statsTimer: ReturnType<typeof setInterval> | null = null
  private reconnectTimer: ReturnType<typeof setTimeout> | null = null
  private backoffMs = 1000
  private stopped = false
  private wantAudio = false
  private keyframeRequested = 0
  private endMessage: string | undefined
  private endIsError = false

  // Stats accumulators (reset each interval).
  private bytes = 0
  private lastArrival = 0
  private deltas: number[] = []
  private lastFramesRendered = 0

  constructor(
    private readonly url: string,
    private readonly renderer: VideoRenderer,
    private readonly events: StreamClientEvents,
  ) {}

  connect(): void {
    this.stopped = false
    this.open('connecting')
  }

  close(): void {
    this.stopped = true
    this.clearTimers()
    this.ws?.close(1000, 'client closed')
    this.ws = null
  }

  /** Attaches (or detaches) the audio player and tells the server to start/stop device audio. */
  setAudio(player: DeviceAudioPlayer | null): void {
    this.audio = player
    this.wantAudio = player !== null
    this.send({ type: player ? 'audio_on' : 'audio_off' })
  }

  claimMic(): void {
    this.send({ type: 'mic_claim' })
  }

  requestKeyframe(): void {
    // Rate-limit: a burst of decode errors should produce one request.
    const now = performance.now()
    if (now - this.keyframeRequested < 500) return
    this.keyframeRequested = now
    this.send({ type: 'keyframe' })
  }

  private open(status: StreamStatus): void {
    this.events.onStatus(status)
    const ws = new WebSocket(this.url)
    ws.binaryType = 'arraybuffer'
    this.ws = ws

    ws.onopen = () => {
      this.backoffMs = 1000
      this.startStats()
    }
    ws.onmessage = (event) => this.onMessage(event)
    ws.onclose = (event) => {
      if (this.ws !== ws) return
      this.ws = null
      this.clearTimers()
      if (this.stopped) return
      this.scheduleReconnect(event.reason || undefined)
    }
    ws.onerror = () => {
      // onclose follows and handles reconnection.
    }
  }

  private onMessage(event: MessageEvent): void {
    if (typeof event.data === 'string') {
      this.onControl(event.data)
      return
    }

    const data = new Uint8Array(event.data as ArrayBuffer)
    this.bytes += data.byteLength

    if (isAudioFrame(data)) {
      const header = readAudioHeader(data)
      if (header) this.audio?.push(data, header)
      return
    }

    const now = performance.now()
    if (this.lastArrival) this.deltas.push(now - this.lastArrival)
    this.lastArrival = now
    this.renderer.push(data)
  }

  private onControl(text: string): void {
    let message: ServerMessage
    try {
      message = JSON.parse(text) as ServerMessage
    } catch {
      console.warn('[stream] bad control message', text)
      return
    }

    switch (message.type) {
      case 'hello':
        this.endMessage = undefined
        this.endIsError = false
        this.events.onStatus('live')
        this.events.onHello(message)
        // Restore audio after a reconnect.
        if (this.wantAudio && message.audio_grant.device) this.send({ type: 'audio_on' })
        break
      case 'audio_state':
        this.events.onAudioState(message.enabled, message.error, message.message)
        break
      case 'mic_claim_result':
        if (!message.granted) this.events.onServerError(message.reason ?? 'not_permitted', 'Microphone not permitted.')
        break
      case 'error':
        this.events.onServerError(message.code, message.message)
        if (message.code === 'not_found') {
          this.stopped = true
          this.events.onStatus('error', message.message)
        }
        break
      case 'stream_ended':
        // The server closes the socket next; onclose reconnects (a restarted pipeline may recover).
        this.endIsError = message.reason === 'capture_failed'
        this.endMessage = this.endIsError ? (message.message ?? 'The capture pipeline stopped.') : undefined
        break
    }
  }

  private scheduleReconnect(reason?: string): void {
    this.events.onStatus(this.endIsError ? 'error' : 'reconnecting', this.endMessage ?? reason)
    const delay = this.backoffMs
    this.backoffMs = Math.min(this.backoffMs * 2, MAX_BACKOFF_MS)
    this.reconnectTimer = setTimeout(() => {
      if (!this.stopped) this.open('reconnecting')
    }, delay)
  }

  private startStats(): void {
    this.bytes = 0
    this.deltas = []
    this.lastFramesRendered = this.renderer.stats().framesRendered
    this.statsTimer = setInterval(() => this.reportStats(), STATS_INTERVAL_MS)
  }

  private reportStats(): void {
    const r = this.renderer.stats()
    const seconds = STATS_INTERVAL_MS / 1000
    const fps = (r.framesRendered - this.lastFramesRendered) / seconds
    this.lastFramesRendered = r.framesRendered

    const mean = this.deltas.length ? this.deltas.reduce((a, b) => a + b, 0) / this.deltas.length : 0
    const jitterMs = this.deltas.length
      ? this.deltas.reduce((a, d) => a + Math.abs(d - mean), 0) / this.deltas.length
      : 0
    const stats: PlaybackStats = {
      decoder: this.renderer.kind,
      fps: Math.round(fps * 10) / 10,
      jitterMs: Math.round(jitterMs * 10) / 10,
      droppedFrames: r.droppedFrames,
      bitrateKbps: Math.round((this.bytes * 8) / seconds / 1000),
      width: r.width,
      height: r.height,
      codec: r.codec,
    }
    this.bytes = 0
    this.deltas = []

    this.events.onStats(stats)
    this.send({
      type: 'stats',
      decoder: stats.decoder,
      fps: stats.fps,
      jitter_ms: stats.jitterMs,
      dropped_frames: stats.droppedFrames,
    })
  }

  private send(message: ClientMessage): void {
    if (this.ws?.readyState === WebSocket.OPEN) this.ws.send(JSON.stringify(message))
  }

  private clearTimers(): void {
    if (this.statsTimer) clearInterval(this.statsTimer)
    if (this.reconnectTimer) clearTimeout(this.reconnectTimer)
    this.statsTimer = null
    this.reconnectTimer = null
  }
}
