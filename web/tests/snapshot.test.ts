import { describe, expect, it } from 'vitest'
import { snapshotFileName } from '../app/lib/stream/snapshot'

describe('snapshotFileName', () => {
  const at = new Date(2026, 9, 8, 14, 2, 33)

  it('slugs the name and stamps local time', () => {
    expect(snapshotFileName('Living Room Roku', at)).toBe('living-room-roku-20261008-140233.png')
  })

  it('drops accents and punctuation', () => {
    expect(snapshotFileName('  Café / Bedroom!! ', at)).toBe('cafe-bedroom-20261008-140233.png')
  })

  it('falls back when nothing is left', () => {
    expect(snapshotFileName('***', at, 'json')).toBe('sixbench-20261008-140233.json')
  })
})
