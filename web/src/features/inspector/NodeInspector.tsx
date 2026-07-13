import { useState } from 'react'
import { NODE_CATALOG, type NodeConfigValue } from '@/domain'
import { useCanvasStore, type DflNode } from '@/state/canvasStore'
import { validateConfigField } from '@/lib/nodeConfigValidation'
import { nodeAccentVar } from '@/features/canvas/nodeAccent'
import { FormField } from '@/components/FormField'
import { TextInput } from '@/components/TextInput'
import { NumberInput } from '@/components/NumberInput'
import { Select } from '@/components/Select'

/**
 * Design-mode config surface for the selected node (components.md §4). The form is generated
 * from the node's `NODE_CATALOG` field descriptors, so it stays in lockstep with the domain and
 * never hardcodes a per-type form. Edits are held in a local draft and pushed to the authoritative
 * `canvasStore` only when valid, with inline validation mirroring the backend rules.
 *
 * The parent keys this component by node id, so selecting a different node remounts it and the
 * draft re-initializes from that node's config — no manual sync effect required.
 */
export function NodeInspector({ node }: { node: DflNode }) {
  const updateNodeConfig = useCanvasStore((s) => s.updateNodeConfig)
  const updateNodeLabel = useCanvasStore((s) => s.updateNodeLabel)

  const meta = NODE_CATALOG[node.data.type]
  const [label, setLabel] = useState(node.data.label)
  const [draft, setDraft] = useState(node.data.config)

  const onLabelChange = (value: string) => {
    setLabel(value)
    updateNodeLabel(node.id, value)
  }

  const onFieldChange = (key: string, value: NodeConfigValue) => {
    setDraft((d) => ({ ...d, [key]: value }))
    const field = meta.configFields.find((f) => f.key === key)
    if (field && !validateConfigField(field, value)) {
      updateNodeConfig(node.id, { [key]: value })
    }
  }

  return (
    <div className="flex flex-col gap-4 p-4">
      <header className="flex items-center gap-2">
        <span
          aria-hidden
          className="h-5 w-1.5 rounded-full"
          style={{ background: nodeAccentVar(node.data.type) }}
        />
        <div className="flex flex-col">
          <span
            className="text-xs font-semibold uppercase tracking-wide"
            style={{ color: nodeAccentVar(node.data.type) }}
          >
            {node.data.type}
          </span>
          <span className="text-xs text-fg-muted">{meta.description}</span>
        </div>
      </header>

      <FormField id="node-label" label="Label">
        <TextInput id="node-label" value={label} onChange={onLabelChange} />
      </FormField>

      {meta.configFields.length === 0 && (
        <p className="text-sm text-fg-muted">
          This node type has no configurable properties.
        </p>
      )}

      {meta.configFields.map((field) => {
        const value = draft[field.key] ?? field.defaultValue
        const error = validateConfigField(field, value)
        const fieldId = `node-config-${field.key}`
        return (
          <FormField
            key={field.key}
            id={fieldId}
            label={field.label}
            help={field.help}
            error={error}
          >
            {field.kind === 'number' && (
              <NumberInput
                id={fieldId}
                value={typeof value === 'number' ? value : NaN}
                min={field.min}
                max={field.max}
                invalid={Boolean(error)}
                onChange={(v) => onFieldChange(field.key, v)}
              />
            )}
            {field.kind === 'text' && (
              <TextInput
                id={fieldId}
                value={String(value)}
                onChange={(v) => onFieldChange(field.key, v)}
              />
            )}
            {field.kind === 'select' && (
              <Select
                id={fieldId}
                value={String(value)}
                options={field.options ?? []}
                onChange={(v) => onFieldChange(field.key, v)}
              />
            )}
          </FormField>
        )
      })}
    </div>
  )
}
