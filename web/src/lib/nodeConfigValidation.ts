import type { NodeConfigField, NodeConfigValue } from '@/domain'

/**
 * Validates a single node-config field value against its descriptor. Mirrors the shape of the
 * backend FluentValidation rules (coding-standards §4.4) and returns a human message when
 * invalid, or `undefined` when the value is acceptable. Pure, so it is unit-testable in
 * isolation (web/src/lib is the home for framework-agnostic helpers).
 */
export function validateConfigField(
  field: NodeConfigField,
  value: NodeConfigValue,
): string | undefined {
  if (field.kind === 'number') {
    if (typeof value !== 'number' || Number.isNaN(value)) {
      return 'Enter a number.'
    }
    if (field.min !== undefined && value < field.min) {
      return `Must be ≥ ${field.min}.`
    }
    if (field.max !== undefined && value > field.max) {
      return `Must be ≤ ${field.max}.`
    }
  }
  return undefined
}
