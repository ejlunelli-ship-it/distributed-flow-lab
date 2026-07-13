import { describe, it, expect } from 'vitest'
import type { NodeConfigField } from '@/domain'
import { validateConfigField } from './nodeConfigValidation'

const numberField: NodeConfigField = {
  key: 'maxLength',
  label: 'Max length',
  kind: 'number',
  defaultValue: 1000,
  min: 0,
  max: 5000,
}

describe('validateConfigField', () => {
  it('accepts an in-range number', () => {
    expect(validateConfigField(numberField, 100)).toBeUndefined()
  })

  it('rejects NaN / non-numbers', () => {
    expect(validateConfigField(numberField, NaN)).toBe('Enter a number.')
    expect(validateConfigField(numberField, 'x')).toBe('Enter a number.')
  })

  it('enforces min and max bounds', () => {
    expect(validateConfigField(numberField, -1)).toBe('Must be ≥ 0.')
    expect(validateConfigField(numberField, 6000)).toBe('Must be ≤ 5000.')
  })

  it('does not constrain text or select fields', () => {
    const text: NodeConfigField = {
      key: 'routingKey',
      label: 'Routing key',
      kind: 'text',
      defaultValue: '',
    }
    expect(validateConfigField(text, '')).toBeUndefined()
  })
})
