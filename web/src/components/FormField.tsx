import type { ReactNode } from 'react'

interface FormFieldProps {
  id: string
  label: string
  help?: string
  error?: string
  children: ReactNode
}

/**
 * A labelled form control with optional help text and an inline validation message. Wires the
 * `<label>`, `aria-describedby`, and `aria-invalid` relationships so validation mirrors the
 * backend rules accessibly (design-system.md §7.3). Purely presentational — driven by props.
 */
export function FormField({
  id,
  label,
  help,
  error,
  children,
}: FormFieldProps) {
  const helpId = help ? `${id}-help` : undefined
  const errorId = error ? `${id}-error` : undefined
  return (
    <div className="flex flex-col gap-1">
      <label htmlFor={id} className="text-sm font-medium text-fg">
        {label}
      </label>
      {children}
      {help && !error && (
        <p id={helpId} className="text-xs text-fg-muted">
          {help}
        </p>
      )}
      {error && (
        <p id={errorId} className="text-xs text-danger">
          {error}
        </p>
      )}
    </div>
  )
}
