import { useUiStore } from '@/state/uiStore'
import { useCanvasStore } from '@/state/canvasStore'

/**
 * The fixed top bar (wireframes.md §1): brand, current scenario name, the Design/Run mode
 * toggle, and global actions. In Sprint 3 only Design mode exists — Run and Save are shown as
 * disabled affordances (persistence lands in Sprint 5) so the frame matches the wireframe without
 * implying capabilities that are not wired yet.
 */
export function TopBar() {
  const theme = useUiStore((s) => s.theme)
  const toggleTheme = useUiStore((s) => s.toggleTheme)
  const togglePalette = useUiStore((s) => s.togglePalette)
  const paletteCollapsed = useUiStore((s) => s.paletteCollapsed)
  const dirty = useCanvasStore((s) => s.dirty)

  return (
    <header className="flex items-center gap-4 border-b border-border bg-surface px-4 py-2">
      <button
        type="button"
        onClick={togglePalette}
        aria-label={paletteCollapsed ? 'Expand palette' : 'Collapse palette'}
        aria-pressed={paletteCollapsed}
        className="rounded-md border border-border px-2 py-1 text-sm text-fg-muted hover:bg-surface-2"
      >
        ☰
      </button>

      <h1 className="text-lg font-semibold text-fg">Distributed Flow Lab</h1>

      <span className="text-sm text-fg-muted">
        Scenario: Untitled{dirty && <span className="text-warning"> •</span>}
      </span>

      <div
        role="group"
        aria-label="Editor mode"
        className="ml-auto flex overflow-hidden rounded-md border border-border text-sm"
      >
        <span className="bg-primary px-3 py-1 font-medium text-primary-contrast">
          Design
        </span>
        <span
          className="px-3 py-1 text-fg-muted opacity-60"
          title="Run mode arrives in Sprint 5"
        >
          Run
        </span>
      </div>

      <button
        type="button"
        disabled
        title="Saving scenarios arrives in Sprint 5"
        className="rounded-md border border-border px-3 py-1 text-sm text-fg-muted opacity-60"
      >
        Save
      </button>

      <button
        type="button"
        onClick={toggleTheme}
        aria-label="Toggle theme"
        className="rounded-md border border-border px-2 py-1 text-sm text-fg-muted hover:bg-surface-2"
      >
        {theme === 'dark' ? '☾' : '☀'}
      </button>
    </header>
  )
}
