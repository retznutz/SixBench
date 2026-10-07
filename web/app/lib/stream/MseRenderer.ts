import JMuxer from 'jmuxer'
import { codecStringFromSps, findSps, isKeyframe } from './annexb'
import type { RendererCallbacks, RendererStats, VideoRenderer } from './VideoRenderer'

/** How far playback may lag the buffered edge before we jump forward, in seconds. */
const MAX_LAG_SECONDS = 0.5

/**
 * Fallback renderer (?decoder=mse): JMuxer wraps the H.264 stream into fMP4 in the browser and plays it
 * through Media Source Extensions (ManagedMediaSource on iOS).
 */
export class MseRenderer implements VideoRenderer {
  readonly kind = 'mse' as const

  private readonly muxer: JMuxer
  private waitingForKeyframe = true
  private droppedFrames = 0
  private codec: string | null = null
  private readonly lagTimer: ReturnType<typeof setInterval>
  private lastPresented = 0

  static isSupported(): boolean {
    return typeof window !== 'undefined' && ('MediaSource' in window || 'ManagedMediaSource' in window)
  }

  constructor(
    private readonly video: HTMLVideoElement,
    private readonly callbacks: RendererCallbacks,
    fps = 30,
  ) {
    video.muted = true
    video.playsInline = true
    video.autoplay = true
    this.muxer = new JMuxer({
      node: video,
      mode: 'video',
      flushingTime: 0,
      fps,
      clearBuffer: true,
      debug: false,
      onError: () => {
        this.waitingForKeyframe = true
        this.callbacks.onNeedKeyframe()
      },
    })
    this.lagTimer = setInterval(() => this.chaseLiveEdge(), 250)
  }

  push(accessUnit: Uint8Array): void {
    if (this.waitingForKeyframe) {
      if (!isKeyframe(accessUnit)) {
        this.droppedFrames++
        return
      }
      const sps = findSps(accessUnit)
      if (sps) this.codec = codecStringFromSps(sps)
      this.waitingForKeyframe = false
    }
    this.muxer.feed({ video: accessUnit })
  }

  stats(): RendererStats {
    const quality = this.video.getVideoPlaybackQuality?.()
    return {
      framesRendered: quality?.totalVideoFrames ?? 0,
      droppedFrames: this.droppedFrames + (quality?.droppedVideoFrames ?? 0),
      width: this.video.videoWidth,
      height: this.video.videoHeight,
      codec: this.codec,
    }
  }

  close(): void {
    clearInterval(this.lagTimer)
    this.muxer.destroy()
  }

  private chaseLiveEdge(): void {
    const { buffered, currentTime } = this.video
    if (buffered.length === 0) return
    const end = buffered.end(buffered.length - 1)
    if (end - currentTime > MAX_LAG_SECONDS) {
      this.video.currentTime = end - 0.05
    }
    if (this.video.paused) {
      this.video.play().catch(() => {
        /* autoplay may need a gesture; the overlay prompts the user */
      })
    }
    // Detect a stalled player (no progress while data arrives) and request a fresh GOP.
    if (currentTime === this.lastPresented && end - currentTime > MAX_LAG_SECONDS * 2) {
      this.callbacks.onNeedKeyframe()
    }
    this.lastPresented = currentTime
  }
}
