import type { DecoderKind } from '../../types/stream-protocol'

export interface RendererStats {
  framesRendered: number
  droppedFrames: number
  width: number
  height: number
  codec: string | null
}

export interface RendererCallbacks {
  /** The decoder lost sync; ask the server to resend from a keyframe. */
  onNeedKeyframe: () => void
  /** A fatal, user-visible error (e.g. unsupported codec). */
  onError: (message: string) => void
}

/** Turns H.264 access units into pictures on screen. */
export interface VideoRenderer {
  readonly kind: DecoderKind
  push(accessUnit: Uint8Array): void
  stats(): RendererStats
  close(): void
}
