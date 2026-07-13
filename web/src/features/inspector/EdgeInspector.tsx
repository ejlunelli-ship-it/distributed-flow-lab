import { useState } from 'react'
import { useCanvasStore, type DflEdge } from '@/state/canvasStore'
import { FormField } from '@/components/FormField'
import { TextInput } from '@/components/TextInput'

/**
 * Design-mode config surface for the selected edge. Shows the directed source→target it connects
 * and lets the user set its label (e.g. a RabbitMQ routing key / binding). Keyed by edge id, so
 * the local draft re-initializes on selection change.
 */
export function EdgeInspector({ edge }: { edge: DflEdge }) {
  const updateEdgeLabel = useCanvasStore((s) => s.updateEdgeLabel)
  const nodes = useCanvasStore((s) => s.nodes)

  const sourceType = nodes.find((n) => n.id === edge.source)?.data.type ?? '—'
  const targetType = nodes.find((n) => n.id === edge.target)?.data.type ?? '—'
  const [label, setLabel] = useState(edge.data?.label ?? '')

  const onLabelChange = (value: string) => {
    setLabel(value)
    updateEdgeLabel(edge.id, value)
  }

  return (
    <div className="flex flex-col gap-4 p-4">
      <header className="flex flex-col gap-1">
        <span className="text-xs font-semibold uppercase tracking-wide text-fg-muted">
          Edge
        </span>
        <span className="text-sm text-fg">
          {sourceType} <span className="text-fg-muted">→</span> {targetType}
        </span>
      </header>

      <FormField
        id="edge-label"
        label="Label"
        help="Shown on the edge — e.g. a routing key or binding."
      >
        <TextInput
          id="edge-label"
          value={label}
          placeholder="routing key"
          onChange={onLabelChange}
        />
      </FormField>
    </div>
  )
}
