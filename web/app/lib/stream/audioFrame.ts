/**
 * 12-byte header on audio messages (mirrors SixBench.Common.Streaming.AudioFrameHeader):
 * 'V' 'A' | flags u8 | reserved u8 | sequence u32 BE | timestamp ms u32 BE.
 * Messages without this header are video.
 */

export const AUDIO_HEADER_SIZE = 12

export enum AudioFrameFlags {
  None = 0,
  Mic = 1,
  Discontinuity = 2,
}

export interface AudioFrameHeader {
  flags: number
  sequence: number
  timestampMs: number
}

const MAGIC_V = 0x56
const MAGIC_A = 0x41

export function isAudioFrame(data: Uint8Array): boolean {
  return data.length >= AUDIO_HEADER_SIZE && data[0] === MAGIC_V && data[1] === MAGIC_A
}

export function readAudioHeader(data: Uint8Array): AudioFrameHeader | null {
  if (!isAudioFrame(data)) return null
  const view = new DataView(data.buffer, data.byteOffset, data.byteLength)
  return {
    flags: data[2]!,
    sequence: view.getUint32(4, false),
    timestampMs: view.getUint32(8, false),
  }
}

export function writeAudioFrame(header: AudioFrameHeader, payload: Uint8Array): Uint8Array {
  const out = new Uint8Array(AUDIO_HEADER_SIZE + payload.length)
  const view = new DataView(out.buffer)
  out[0] = MAGIC_V
  out[1] = MAGIC_A
  out[2] = header.flags & 0xff
  out[3] = 0
  view.setUint32(4, header.sequence >>> 0, false)
  view.setUint32(8, header.timestampMs >>> 0, false)
  out.set(payload, AUDIO_HEADER_SIZE)
  return out
}
