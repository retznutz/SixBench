/** H.264 Annex-B helpers for the browser side of the stream. */

export const NAL_SLICE = 1
export const NAL_IDR = 5
export const NAL_SEI = 6
export const NAL_SPS = 7
export const NAL_PPS = 8
export const NAL_AUD = 9

/**
 * Splits an Annex-B buffer into NAL unit payloads (start codes removed).
 * The returned arrays are views into `data`.
 */
export function splitNalUnits(data: Uint8Array): Uint8Array[] {
  const units: Uint8Array[] = []
  let start = -1
  let i = 0
  while (i + 2 < data.length) {
    if (data[i] === 0 && data[i + 1] === 0 && data[i + 2] === 1) {
      if (start >= 0) {
        // Trailing zero belongs to a 4-byte start code of the next unit.
        let end = i
        if (end > start && data[end - 1] === 0) end--
        units.push(data.subarray(start, end))
      }
      start = i + 3
      i += 3
    } else {
      i++
    }
  }
  if (start >= 0 && start < data.length) units.push(data.subarray(start))
  return units
}

/** NAL unit type (low five bits of the header byte). */
export function nalType(nal: Uint8Array): number {
  return nal.length > 0 ? nal[0]! & 0x1f : -1
}

/** True when the access unit contains an IDR slice. */
export function isKeyframe(data: Uint8Array): boolean {
  return splitNalUnits(data).some((nal) => nalType(nal) === NAL_IDR)
}

/** Returns the first SPS NAL unit in the access unit, if any. */
export function findSps(data: Uint8Array): Uint8Array | undefined {
  return splitNalUnits(data).find((nal) => nalType(nal) === NAL_SPS)
}

/**
 * Builds the WebCodecs codec string (`avc1.PPCCLL`) from an SPS: profile_idc, constraint flags and level_idc.
 * Deriving it from the stream (instead of hard-coding `avc1.42001e`) lets 1080p and higher levels decode.
 */
export function codecStringFromSps(sps: Uint8Array): string {
  if (sps.length < 4) throw new Error('SPS too short')
  const hex = (b: number) => b.toString(16).padStart(2, '0')
  return `avc1.${hex(sps[1]!)}${hex(sps[2]!)}${hex(sps[3]!)}`
}
