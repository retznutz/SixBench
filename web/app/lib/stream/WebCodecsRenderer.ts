import { codecStringFromSps, findSps, isKeyframe } from './annexb'
import type { RendererCallbacks, RendererStats, VideoRenderer } from './VideoRenderer'

/** Decode queue depth beyond which we reset rather than fall further behind. */
const MAX_DECODE_QUEUE = 30

/**
 * Default renderer: WebCodecs VideoDecoder (Annex-B, optimizeForLatency) drawing the newest frame to a canvas.
 */
export class WebCodecsRenderer implements VideoRenderer {
  readonly kind = 'webcodecs' as const

  private decoder: VideoDecoder | null = null
  private codec: string | null = null
  private waitingForKeyframe = true
  private pendingFrame: VideoFrame | null = null
  private rafId = 0
  private framesRendered = 0
  private droppedFrames = 0
  private width = 0
  private height = 0
  private timestamp = 0
  private closed = false
  private readonly ctx: CanvasRenderingContext2D

  static isSupported(): boolean {
    return typeof window !== 'undefined' && 'VideoDecoder' in window && 'EncodedVideoChunk' in window
  }

  constructor(
    private readonly canvas: HTMLCanvasElement,
    private readonly callbacks: RendererCallbacks,
  ) {
    const ctx = canvas.getContext('2d', { alpha: false, desynchronized: true })
    if (!ctx) throw new Error('Canvas 2D context is unavailable.')
    this.ctx = ctx
  }

  push(accessUnit: Uint8Array): void {
    if (this.closed) return
    const key = isKeyframe(accessUnit)

    if (this.waitingForKeyframe) {
      if (!key) {
        this.droppedFrames++
        return
      }
      const sps = findSps(accessUnit)
      const codec = sps ? codecStringFromSps(sps) : this.codec
      if (!codec) {
        this.droppedFrames++
        return
      }
      if (!this.decoder || this.decoder.state === 'closed' || codec !== this.codec) {
        this.configure(codec)
      }
      this.waitingForKeyframe = false
    }

    const decoder = this.decoder
    if (!decoder || decoder.state !== 'configured') return

    if (decoder.decodeQueueSize > MAX_DECODE_QUEUE) {
      // Hopelessly behind (background tab, slow device): start over at the next keyframe.
      this.resync()
      return
    }

    // Timestamps only need to be monotonic; frames are drawn as soon as they decode.
    this.timestamp += 1
    try {
      decoder.decode(
        new EncodedVideoChunk({ type: key ? 'key' : 'delta', timestamp: this.timestamp, data: accessUnit }),
      )
    } catch (err) {
      console.warn('[stream] decode() threw', err)
      this.resync()
    }
  }

  stats(): RendererStats {
    return {
      framesRendered: this.framesRendered,
      droppedFrames: this.droppedFrames,
      width: this.width,
      height: this.height,
      codec: this.codec,
    }
  }

  close(): void {
    this.closed = true
    cancelAnimationFrame(this.rafId)
    this.pendingFrame?.close()
    this.pendingFrame = null
    if (this.decoder && this.decoder.state !== 'closed') this.decoder.close()
    this.decoder = null
  }

  private configure(codec: string): void {
    if (this.decoder && this.decoder.state !== 'closed') this.decoder.close()
    this.codec = codec
    this.decoder = new VideoDecoder({
      output: (frame) => this.onFrame(frame),
      error: (err) => {
        console.warn('[stream] VideoDecoder error', err)
        if (!this.closed) this.resync()
      },
    })
    try {
      this.decoder.configure({ codec, optimizeForLatency: true, hardwareAcceleration: 'no-preference' })
    } catch (err) {
      this.callbacks.onError(`This browser cannot decode ${codec}: ${(err as Error).message}`)
    }
  }

  private resync(): void {
    this.droppedFrames++
    this.waitingForKeyframe = true
    if (this.decoder && this.decoder.state !== 'closed') this.decoder.close()
    this.decoder = null
    this.callbacks.onNeedKeyframe()
  }

  private onFrame(frame: VideoFrame): void {
    if (this.closed) {
      frame.close()
      return
    }
    // Keep only the newest frame; anything older is stale by the time we paint.
    if (this.pendingFrame) {
      this.pendingFrame.close()
      this.droppedFrames++
    }
    this.pendingFrame = frame
    if (!this.rafId) this.rafId = requestAnimationFrame(() => this.draw())
  }

  private draw(): void {
    this.rafId = 0
    const frame = this.pendingFrame
    if (!frame) return
    this.pendingFrame = null
    if (this.canvas.width !== frame.displayWidth || this.canvas.height !== frame.displayHeight) {
      this.canvas.width = frame.displayWidth
      this.canvas.height = frame.displayHeight
      this.width = frame.displayWidth
      this.height = frame.displayHeight
    }
    this.ctx.drawImage(frame, 0, 0)
    frame.close()
    this.framesRendered++
  }
}
