import { useEffect } from 'react'
import { BrowserRouter } from 'react-router-dom'
import { AppRoutes } from './AppRoutes'
import { useUiStore } from '@/state/uiStore'

/**
 * Application root: applies the active theme to the document element (so the CSS token overrides
 * in index.css take effect) and mounts the router. From Sprint 3 the default route is the
 * Scenario Editor (Design mode).
 */
export function App() {
  const theme = useUiStore((s) => s.theme)

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', theme)
  }, [theme])

  return (
    <BrowserRouter>
      <AppRoutes />
    </BrowserRouter>
  )
}
