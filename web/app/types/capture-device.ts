import type { EncoderLink } from './encoder-link'

export type HostPlatform = 'Unknown' | 'Windows' | 'MacOS' | 'Linux'

/** An HDMI capture encoder (GET /capture-devices). */
export interface CaptureDevice {
  /** URL-safe key used in routes. */
  id: string
  /** Platform identifier that survives re-plugging. */
  stableId: string
  name: string
  videoInput: string
  audioInput: string | null
  platform: HostPlatform
  isConnected: boolean
  link: EncoderLink | null
}

/** An audio capture device (GET /capture-devices/audio-inputs). */
export interface AudioInput {
  name: string
  input: string
}
