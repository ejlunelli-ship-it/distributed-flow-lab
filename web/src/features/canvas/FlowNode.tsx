import { memo } from 'react'
import { Handle, Position, type NodeProps } from '@xyflow/react'
import type { DflNode } from '@/state/canvasStore'
import { nodeAccentVar } from './nodeAccent'

/**
 * The single custom React Flow node. One data-driven component renders every `NodeType`
 * variant (its accent, type caption, and label) rather than 14 near-identical components —
 * composition over duplication (CLAUDE.md Frontend Principles). Runtime badges (queue depth,
 * circuit-breaker state) are layered on in Run mode from Sprint 5. Memoized so a change to one
 * node does not re-render the whole graph (ADR-001 performance budget).
 */
function FlowNodeComponent({ data, selected }: NodeProps<DflNode>) {
  const accent = nodeAccentVar(data.type)
  return (
    <div
      role="group"
      aria-label={`${data.type} node ${data.label}`}
      className="min-w-[9rem] rounded-md border bg-surface shadow-sm transition-shadow"
      style={{
        borderColor: selected
          ? 'var(--color-focus-ring)'
          : 'var(--color-border)',
        boxShadow: selected ? `0 0 0 2px var(--color-focus-ring)` : undefined,
      }}
    >
      <Handle
        type="target"
        position={Position.Left}
        className="!h-2 !w-2 !border-0"
        style={{ background: accent }}
      />
      <div className="flex items-center gap-2 px-3 py-2">
        <span
          aria-hidden
          className="h-6 w-1.5 rounded-full"
          style={{ background: accent }}
        />
        <span className="flex flex-col">
          <span
            className="text-xs font-medium uppercase tracking-wide"
            style={{ color: accent }}
          >
            {data.type}
          </span>
          <span className="text-sm text-fg">{data.label}</span>
        </span>
      </div>
      <Handle
        type="source"
        position={Position.Right}
        className="!h-2 !w-2 !border-0"
        style={{ background: accent }}
      />
    </div>
  )
}

export const FlowNode = memo(FlowNodeComponent)
