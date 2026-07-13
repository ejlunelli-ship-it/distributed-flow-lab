import { memo } from 'react'
import {
  BaseEdge,
  EdgeLabelRenderer,
  getBezierPath,
  type EdgeProps,
} from '@xyflow/react'
import type { DflEdge } from '@/state/canvasStore'

/**
 * The custom directed edge. It draws the bezier track between two nodes and renders the edge
 * label (e.g. a routing key / binding). In Run mode this same edge hosts the animated message
 * token driven by backend events (Sprint 5) — kept behind our own component so the React Flow
 * dependency stays isolated (ADR-001). Memoized to localize re-renders.
 */
function FlowEdgeComponent({
  id,
  sourceX,
  sourceY,
  targetX,
  targetY,
  sourcePosition,
  targetPosition,
  markerEnd,
  selected,
  data,
}: EdgeProps<DflEdge>) {
  const [path, labelX, labelY] = getBezierPath({
    sourceX,
    sourceY,
    targetX,
    targetY,
    sourcePosition,
    targetPosition,
  })
  const label = data?.label ?? ''

  return (
    <>
      <BaseEdge
        id={id}
        path={path}
        markerEnd={markerEnd}
        style={{
          stroke: selected ? 'var(--color-focus-ring)' : 'var(--color-border)',
          strokeWidth: selected ? 2 : 1.5,
        }}
      />
      {label && (
        <EdgeLabelRenderer>
          <div
            className="pointer-events-none absolute rounded bg-surface-2 px-1.5 py-0.5 text-xs text-fg-muted"
            style={{
              transform: `translate(-50%, -50%) translate(${labelX}px, ${labelY}px)`,
            }}
          >
            {label}
          </div>
        </EdgeLabelRenderer>
      )}
    </>
  )
}

export const FlowEdge = memo(FlowEdgeComponent)
