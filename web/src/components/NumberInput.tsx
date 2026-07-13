interface NumberInputProps {
  id: string
  value: number
  onChange: (value: number) => void
  min?: number
  max?: number
  invalid?: boolean
}

const baseClass =
  'rounded-md border bg-surface-2 px-2 py-1.5 text-sm text-fg focus:outline-none focus-visible:ring-2 focus-visible:ring-focus'

/**
 * A styled numeric input (design-system primitive). Emits `NaN` for empty/invalid text so the
 * caller can render a validation message rather than silently coercing to 0.
 */
export function NumberInput({
  id,
  value,
  onChange,
  min,
  max,
  invalid,
}: NumberInputProps) {
  return (
    <input
      id={id}
      type="number"
      value={Number.isNaN(value) ? '' : value}
      min={min}
      max={max}
      aria-invalid={invalid || undefined}
      onChange={(e) =>
        onChange(e.target.value === '' ? NaN : e.target.valueAsNumber)
      }
      className={baseClass}
      style={{
        borderColor: invalid ? 'var(--color-danger)' : 'var(--color-border)',
      }}
    />
  )
}
