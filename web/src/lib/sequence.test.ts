import { describe, it, expect } from 'vitest'
import { findSequenceGap } from './sequence'

describe('findSequenceGap', () => {
  it('returns no gap for the expected next sequence', () => {
    expect(findSequenceGap(41, 42)).toEqual([])
  })

  it('returns the missing sequences when events are skipped', () => {
    expect(findSequenceGap(41, 45)).toEqual([42, 43, 44])
  })

  it('returns no gap for a duplicate or older sequence', () => {
    expect(findSequenceGap(41, 41)).toEqual([])
    expect(findSequenceGap(41, 30)).toEqual([])
  })

  it('handles the first-ever event (no prior sequence)', () => {
    expect(findSequenceGap(-1, 0)).toEqual([])
    expect(findSequenceGap(-1, 3)).toEqual([0, 1, 2])
  })
})
