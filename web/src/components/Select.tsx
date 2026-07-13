interface SelectProps {
  id: string
  value: string
  options: readonly string[]
  onChange: (value: string) => void
}

/** A styled single-choice select (design-system primitive). Controlled, props-driven. */
export function Select({ id, value, options, onChange }: SelectProps) {
  return (
    <select
      id={id}
      value={value}
      onChange={(e) => onChange(e.target.value)}
      className="rounded-md border border-border bg-surface-2 px-2 py-1.5 text-sm text-fg focus:outline-none focus-visible:ring-2 focus-visible:ring-focus"
    >
      {options.map((option) => (
        <option key={option} value={option}>
          {option}
        </option>
      ))}
    </select>
  )
}
