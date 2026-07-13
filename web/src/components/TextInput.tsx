interface TextInputProps {
  id: string
  value: string
  onChange: (value: string) => void
  invalid?: boolean
  placeholder?: string
}

const baseClass =
  'rounded-md border bg-surface-2 px-2 py-1.5 text-sm text-fg placeholder:text-fg-muted focus:outline-none focus-visible:ring-2 focus-visible:ring-focus'

/** A styled single-line text input (design-system primitive). Controlled, props-driven. */
export function TextInput({
  id,
  value,
  onChange,
  invalid,
  placeholder,
}: TextInputProps) {
  return (
    <input
      id={id}
      type="text"
      value={value}
      placeholder={placeholder}
      aria-invalid={invalid || undefined}
      onChange={(e) => onChange(e.target.value)}
      className={baseClass}
      style={{
        borderColor: invalid ? 'var(--color-danger)' : 'var(--color-border)',
      }}
    />
  )
}
