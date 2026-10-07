/**
 * Stream WebSocket protocol (/api/v1/streams/{id}/ws).
 * Binary frames: raw H.264 Annex-B access units, or Opus packets prefixed with a 12-byte "VA" header.
 * Text frames: the JSON control messages below (snake_case).
 */

export type DecoderKind = 'webcodecs' | 'mse'

export type StreamStatus = 'idle' | 'connecting' | 'live' | 'reconnecting' | 'ended' | 'error' | 'unsupported'

export interface AudioGrant {
  device: boolean
  mic: boolean
}

export type StreamErrorCode =
  'not_permitted' | 'mic_busy' | 'audio_unavailable' | 'bad_message' | 'capture_failed' | 'not_found'

export interface HelloMessage {
  type: 'hello'
  session_id: string
  stable_id: string
  name: string
  audio_grant: AudioGrant
}

export interface AudioStateMessage {
  type: 'audio_state'
  enabled: boolean
  error?: StreamErrorCode
  message?: string
}

export interface MicClaimResultMessage {
  type: 'mic_claim_result'
  granted: boolean
  reason?: StreamErrorCode
}

export interface ErrorMessage {
  type: 'error'
  code: StreamErrorCode
  message: string
}

export interface StreamEndedMessage {
  type: 'stream_ended'
  reason: 'capture_failed' | 'settings_changed' | 'shutdown' | 'idle' | string
  message?: string
}

export type ServerMessage = HelloMessage | AudioStateMessage | MicClaimResultMessage | ErrorMessage | StreamEndedMessage

export type ClientMessage =
  | { type: 'audio_on' }
  | { type: 'audio_off' }
  | { type: 'mic_claim' }
  | { type: 'mic_release' }
  | { type: 'keyframe' }
  | { type: 'stats'; decoder: DecoderKind; fps: number; jitter_ms: number; dropped_frames: number }

/** Playback statistics shown in the stats overlay. */
export interface PlaybackStats {
  decoder: DecoderKind
  fps: number
  jitterMs: number
  droppedFrames: number
  bitrateKbps: number
  width: number
  height: number
  codec: string | null
}
