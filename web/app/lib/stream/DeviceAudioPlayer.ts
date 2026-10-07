import { AUDIO_HEADER_SIZE, AudioFrameFlags, type AudioFrameHeader } from './audioFrame'

/** Schedule audio this far ahead of the playhead, in seconds. */
const TARGET_LEAD = 0.08
/** If queued audio exceeds this, drop packets until we catch up, in seconds. */
const MAX_LEAD = 0.25

interface SinkCapableAudioContext extends AudioContext {
  setSinkId?: (sinkId: string) => Promise<void>
}

/**
 * Plays device audio: Opus packets → WebCodecs AudioDecoder → low-latency AudioContext.
 */
export class DeviceAudioPlayer {
  private ctx: SinkCapableAudioContext | null = null
  private decoder: AudioDecoder | null = null
  private nextTime = 0
  private dropped = 0

  constructor(private readonly onError: (message: string) => void) {}

  static isSupported(): boolean {
    return typeof window !== 'undefined' && 'AudioDecoder' in window && 'AudioContext' in window
  }

  static supportsOutputSelection(): boolean {
    return typeof AudioContext !== 'undefined' && 'setSinkId' in AudioContext.prototype
  }

  get droppedPackets(): number {
    return this.dropped
  }

  /** Must be called from a user gesture so the AudioContext may start. */
  async start(sinkId?: string): Promise<void> {
    if (!DeviceAudioPlayer.isSupported()) throw new Error('audio:unavailable')
    const config: AudioDecoderConfig = { codec: 'opus', sampleRate: 48000, numberOfChannels: 2 }
    const support = await AudioDecoder.isConfigSupported(config)
    if (!support.supported) throw new Error('audio:unavailable')

    this.ctx = new AudioContext({ latencyHint: 'interactive', sampleRate: 48000 }) as SinkCapableAudioContext
    if (sinkId) await this.setOutputDevice(sinkId)
    await this.ctx.resume()

    this.decoder = new AudioDecoder({
      output: (data) => this.play(data),
      error: (err) => this.onError(`Audio decoder error: ${err.message}`),
    })
    this.decoder.configure(config)
    this.nextTime = 0
  }

  /** Feeds one framed message (header + Opus payload). */
  push(frame: Uint8Array, header: AudioFrameHeader): void {
    const decoder = this.decoder
    if (!decoder || decoder.state !== 'configured') return
    if (header.flags & AudioFrameFlags.Discontinuity) this.nextTime = 0
    decoder.decode(
      new EncodedAudioChunk({
        type: 'key',
        timestamp: header.timestampMs * 1000,
        data: frame.subarray(AUDIO_HEADER_SIZE),
      }),
    )
  }

  async setOutputDevice(sinkId: string): Promise<void> {
    if (this.ctx?.setSinkId) await this.ctx.setSinkId(sinkId === 'default' ? '' : sinkId)
  }

  async stop(): Promise<void> {
    if (this.decoder && this.decoder.state !== 'closed') this.decoder.close()
    this.decoder = null
    const ctx = this.ctx
    this.ctx = null
    if (ctx && ctx.state !== 'closed') await ctx.close()
  }

  private play(data: AudioData): void {
    const ctx = this.ctx
    if (!ctx) {
      data.close()
      return
    }

    const now = ctx.currentTime
    if (this.nextTime < now) {
      // Underrun or first packet: re-establish the lead.
      this.nextTime = now + TARGET_LEAD
    } else if (this.nextTime - now > MAX_LEAD) {
      // Latency is building up: drop this packet to catch up.
      this.dropped++
      data.close()
      return
    }

    const buffer = ctx.createBuffer(data.numberOfChannels, data.numberOfFrames, data.sampleRate)
    for (let ch = 0; ch < data.numberOfChannels; ch++) {
      const plane = new Float32Array(data.numberOfFrames)
      data.copyTo(plane, { planeIndex: ch, format: 'f32-planar' })
      buffer.copyToChannel(plane, ch)
    }
    data.close()

    const source = ctx.createBufferSource()
    source.buffer = buffer
    source.connect(ctx.destination)
    source.start(this.nextTime)
    this.nextTime += buffer.duration
  }
}
