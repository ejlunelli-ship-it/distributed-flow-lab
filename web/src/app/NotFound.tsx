import { Link } from 'react-router-dom'

/** Graceful fallback for unknown routes (screens.md §3.5). */
export function NotFound() {
  return (
    <div className="flex h-full flex-col items-center justify-center gap-4 bg-page text-fg">
      <h1 className="text-2xl font-semibold">Page not found</h1>
      <Link to="/editor/new" className="text-primary underline">
        Go to the editor
      </Link>
    </div>
  )
}
