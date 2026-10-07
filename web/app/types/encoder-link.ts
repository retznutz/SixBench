import type { RokuDevice } from './roku-device'

/** Saved encoder settings and its Roku (GET /encoder-links). */
export interface EncoderLink {
  id: number
  captureDeviceStableId: string
  displayName: string
  videoInput: string
  audioInput: string | null
  rokuDeviceId: number | null
  rokuDevice: RokuDevice | null
  allowDeviceAudio: boolean
  frameRate: number | null
  videoSize: string | null
  pixelFormat: string | null
  createdUtc: string
  updatedUtc: string
}

/** Body for PUT /encoder-links/{id}. */
export interface UpsertEncoderLinkRequest {
  displayName: string
  videoInput?: string | null
  audioInput?: string | null
  rokuDeviceId?: number | null
  allowDeviceAudio: boolean
  frameRate?: number | null
  videoSize?: string | null
  pixelFormat?: string | null
}
