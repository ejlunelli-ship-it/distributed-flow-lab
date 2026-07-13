import { useCanvasStore } from '@/state/canvasStore'
import { NodeInspector } from './NodeInspector'
import { EdgeInspector } from './EdgeInspector'

/**
 * The right rail. In Design mode it hosts the node/edge config surface; the Event and Metrics
 * tabs (components.md §4) arrive in Run mode (Sprint 5). Selecting a node shows the NodeInspector,
 * an edge shows the EdgeInspector, and nothing selected shows a hint. Each inspector is keyed by
 * the selected id so its local draft re-initializes on selection change.
 */
export function InspectorPanel() {
  const selectedNodeId = useCanvasStore((s) => s.selectedNodeId)
  const selectedEdgeId = useCanvasStore((s) => s.selectedEdgeId)
  const node = useCanvasStore((s) =>
    s.nodes.find((n) => n.id === s.selectedNodeId),
  )
  const edge = useCanvasStore((s) =>
    s.edges.find((e) => e.id === s.selectedEdgeId),
  )

  return (
    <aside
      aria-label="Inspector"
      className="h-full w-72 overflow-y-auto border-l border-border bg-surface"
    >
      <div className="border-b border-border px-4 py-3">
        <h2 className="text-sm font-semibold text-fg">Inspector</h2>
      </div>
      {node && selectedNodeId ? (
        <NodeInspector key={selectedNodeId} node={node} />
      ) : edge && selectedEdgeId ? (
        <EdgeInspector key={selectedEdgeId} edge={edge} />
      ) : (
        <p className="p-4 text-sm text-fg-muted">
          Select a node or edge to edit its configuration.
        </p>
      )}
    </aside>
  )
}
