import { ReactFlowProvider } from '@xyflow/react'
import { TopBar } from './TopBar'
import { NodePalette } from '@/features/canvas/NodePalette'
import { CanvasEditor } from '@/features/canvas/CanvasEditor'
import { InspectorPanel } from '@/features/inspector/InspectorPanel'

/**
 * The Scenario Editor screen — Design mode of the app shell (wireframes.md §3, screens.md §3.3).
 * Five-region frame reduced to the three Design-mode regions for Sprint 3: palette (left), canvas
 * (center), inspector (right). The bottom simulation dock is added with Run mode in Sprint 5.
 *
 * `ReactFlowProvider` wraps the surface so `CanvasEditor` can use `useReactFlow()` for
 * screen→flow coordinate projection on drop.
 */
export function EditorScreen() {
  return (
    <ReactFlowProvider>
      <div className="flex h-full flex-col bg-page">
        <TopBar />
        <div className="flex min-h-0 flex-1">
          <NodePalette />
          <main aria-label="Canvas" className="min-w-0 flex-1">
            <CanvasEditor />
          </main>
          <InspectorPanel />
        </div>
      </div>
    </ReactFlowProvider>
  )
}
