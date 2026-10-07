import { describe, expect, it } from 'vitest'
import { codecStringFromSps, findSps, isKeyframe, nalType, splitNalUnits } from '../app/lib/stream/annexb'

const SPS = [0x67, 0x42, 0xc0, 0x28, 0xda, 0x01]
const PPS = [0x68, 0xce, 0x3c, 0x80]
const IDR = [0x65, 0x88, 0x84, 0x00]
const SLICE = [0x41, 0x9a, 0x02]
const AUD = [0x09, 0xf0]

const four = [0, 0, 0, 1]
const three = [0, 0, 1]

function au(...nals: number[][]): Uint8Array {
  return new Uint8Array(nals.flatMap((n, i) => [...(i % 2 ? three : four), ...n]))
}

describe('splitNalUnits', () => {
  it('splits 3- and 4-byte start codes and strips them', () => {
    const units = splitNalUnits(au(AUD, SPS, PPS, IDR))
    expect(units.map((u) => Array.from(u))).toEqual([AUD, SPS, PPS, IDR])
  })

  it('returns nothing for data without a start code', () => {
    expect(splitNalUnits(new Uint8Array([1, 2, 3, 4]))).toEqual([])
  })
})

describe('keyframes and SPS', () => {
  it('detects IDR access units', () => {
    expect(isKeyframe(au(AUD, SPS, PPS, IDR))).toBe(true)
    expect(isKeyframe(au(AUD, SLICE))).toBe(false)
  })

  it('finds the SPS', () => {
    const sps = findSps(au(AUD, SPS, PPS, IDR))
    expect(sps && nalType(sps)).toBe(7)
    expect(findSps(au(AUD, SLICE))).toBeUndefined()
  })
})

describe('codecStringFromSps', () => {
  it('builds avc1.PPCCLL from profile, constraints and level', () => {
    // Constrained Baseline (66 / 0xc0), level 4.0 (40 = 0x28) — 1080p capable.
    expect(codecStringFromSps(new Uint8Array(SPS))).toBe('avc1.42c028')
  })

  it('matches the classic baseline 3.0 string', () => {
    expect(codecStringFromSps(new Uint8Array([0x67, 0x42, 0x00, 0x1e]))).toBe('avc1.42001e')
  })

  it('rejects a truncated SPS', () => {
    expect(() => codecStringFromSps(new Uint8Array([0x67, 0x42]))).toThrow()
  })
})
