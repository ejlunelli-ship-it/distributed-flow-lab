import { Navigate, Route, Routes } from 'react-router-dom'
import { EditorScreen } from './EditorScreen'
import { DevScreen } from './DevScreen'
import { NotFound } from './NotFound'

/**
 * Route map (screens.md §1). Sprint 3 ships the Design-mode editor; `/catalog` and
 * `/simulate/:id` arrive with their own sprints. `/dev` retains the Sprint 2 realtime harness.
 */
export function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/editor/new" replace />} />
      <Route path="/editor/new" element={<EditorScreen />} />
      <Route path="/editor/:scenarioId" element={<EditorScreen />} />
      <Route path="/dev" element={<DevScreen />} />
      <Route path="*" element={<NotFound />} />
    </Routes>
  )
}
