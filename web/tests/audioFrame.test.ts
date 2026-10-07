import { describe, expect, it } from 'vitest'
import {
  AUDIO_HEADER_SIZE,
  AudioFrameFlags,
  isAudioFrame,
  readAudioHeader,
  writeAudioFrame,
} from '../app/lib/stream/audioFrame'

describe('audio frame header', () => {
  it('round-trips flags, sequence and timestamp', () => {
    const payload = new Uint8Array([0xfc, 0xff, 0xfe])
    const frame = writeAudioFrame(
      { flags: AudioFrameFlags.Discontinuity, sequence: 0xfffffffe, timestampMs: 123456 },
      payload,
    )

    expect(frame.length).toBe(AUDIO_HEADER_SIZE + payload.length)
    expect(Array.from(frame.subarray(0, 4))).toEqual([0x56, 0x41, 2, 0])
    expect(readAudioHeader(frame)).toEqual({ flags: 2, sequence: 0xfffffffe, timestampMs: 123456 })
    expect(Array.from(frame.subarray(AUDIO_HEADER_SIZE))).toEqual(Array.from(payload))
  })

  it('matches the server byte layout (big-endian)', () => {
    // Same bytes as SixBench.Tests AudioFrameHeaderTests.
    const bytes = new Uint8Array([0x56, 0x41, 0x01, 0x00, 0x00, 0x00, 0x01, 0x02, 0x00, 0x00, 0x03, 0xe8])
    expect(readAudioHeader(bytes)).toEqual({ flags: AudioFrameFlags.Mic, sequence: 258, timestampMs: 1000 })
  })

  it('treats Annex-B video as not audio', () => {
    expect(isAudioFrame(new Uint8Array([0, 0, 0, 1, 0x09, 0xf0, 0, 0, 0, 1, 0x65, 0x88]))).toBe(false)
    expect(readAudioHeader(new Uint8Array([0x56, 0x41]))).toBeNull()
  })
})
