import { create } from 'zustand'

export type Theme = 'dark' | 'light'

/**
 * Ephemeral UI state shared across the shell (components.md §0 `uiStore`): theme, palette
 * collapse, and a transient connection-validation message surfaced inline when the canvas
 * rejects an illegal edge. This store holds presentation concerns only — never domain or
 * server truth.
 */
interface UiState {
  theme: Theme
  paletteCollapsed: boolean
  /** Transient message shown when a connection attempt is rejected; auto-cleared by the UI. */
  connectionError: string | null

  toggleTheme: () => void
  setTheme: (theme: Theme) => void
  togglePalette: () => void
  setConnectionError: (message: string | null) => void
}

export const useUiStore = create<UiState>((set) => ({
  theme: 'dark',
  paletteCollapsed: false,
  connectionError: null,

  toggleTheme: () =>
    set((s) => ({ theme: s.theme === 'dark' ? 'light' : 'dark' })),
  setTheme: (theme) => set({ theme }),
  togglePalette: () => set((s) => ({ paletteCollapsed: !s.paletteCollapsed })),
  setConnectionError: (connectionError) => set({ connectionError }),
}))
