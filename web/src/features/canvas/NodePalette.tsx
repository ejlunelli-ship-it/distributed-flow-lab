import type { DragEvent } from 'react'
import {
  NODE_CATALOG,
  PALETTE_GROUPS,
  nodeTypesInGroup,
  type NodeType,
} from '@/domain'
import { useCanvasStore } from '@/state/canvasStore'
import { useUiStore } from '@/state/uiStore'
import { NODE_DND_MIME, nodeAccentVar } from './nodeAccent'

/**
 * The left rail: a source of draggable `NodeType`s grouped by family (Compute, Networking,
 * Messaging, Data). Groups and items are derived entirely from the domain `NODE_CATALOG`, so a
 * new node type appears here automatically. Each item can be dragged onto the canvas *or*
 * activated (click / Enter) to drop at a staggered default position — the latter keeps the
 * palette fully keyboard-operable (WCAG, design-system.md §7.2) and unit-testable without HTML5
 * drag-and-drop.
 */
export function NodePalette() {
  const collapsed = useUiStore((s) => s.paletteCollapsed)
  const addNode = useCanvasStore((s) => s.addNode)
  const nodeCount = useCanvasStore((s) => s.nodes.length)

  const onDragStart = (event: DragEvent<HTMLButtonElement>, type: NodeType) => {
    event.dataTransfer.setData(NODE_DND_MIME, type)
    event.dataTransfer.effectAllowed = 'move'
  }

  const onActivate = (type: NodeType) => {
    // Click-placed nodes march left→right so a pipeline reads naturally and successive
    // drops never overlap (drag-and-drop places nodes at the pointer instead).
    addNode(type, { x: 80 + nodeCount * 200, y: 150 })
  }

  return (
    <nav
      aria-label="Node palette"
      className="flex h-full flex-col gap-4 overflow-y-auto border-r border-border bg-surface p-3"
      style={{ width: collapsed ? '3.5rem' : '15rem' }}
    >
      {PALETTE_GROUPS.map((group) => (
        <section key={group}>
          {!collapsed && (
            <h2 className="mb-2 px-1 text-xs font-semibold uppercase tracking-wide text-fg-muted">
              {group}
            </h2>
          )}
          <ul className="flex flex-col gap-1">
            {nodeTypesInGroup(group).map((type) => {
              const accent = nodeAccentVar(type)
              return (
                <li key={type}>
                  <button
                    type="button"
                    draggable
                    onDragStart={(e) => onDragStart(e, type)}
                    onClick={() => onActivate(type)}
                    title={NODE_CATALOG[type].description}
                    aria-label={`Add ${type} node`}
                    className="flex w-full cursor-grab items-center gap-2 rounded-md border border-transparent px-2 py-1.5 text-left text-sm text-fg hover:border-border hover:bg-surface-2 focus:outline-none focus-visible:ring-2 focus-visible:ring-focus active:cursor-grabbing"
                  >
                    <span
                      aria-hidden
                      className="h-4 w-1.5 shrink-0 rounded-full"
                      style={{ background: accent }}
                    />
                    {!collapsed && <span>{type}</span>}
                  </button>
                </li>
              )
            })}
          </ul>
        </section>
      ))}
    </nav>
  )
}
