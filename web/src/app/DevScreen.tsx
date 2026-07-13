import { useEffect, useState } from 'react'
import { SimulationDevPanel } from '@/features/simulation/SimulationDevPanel'

type ApiHealth = 'checking' | 'ok' | 'unreachable'

/**
 * Developer screen (`/dev`) retaining the Sprint 2 realtime verification harness: it checks the
 * API health through the dev proxy and drives the demo simulation over SignalR. Superseded by the
 * canvas → Run-mode simulation flow in Sprint 5; kept here until then so the realtime pipeline
 * stays exercisable end-to-end.
 */
export function DevScreen() {
  const [health, setHealth] = useState<ApiHealth>('checking')

  useEffect(() => {
    const controller = new AbortController()
    fetch('/health', { signal: controller.signal })
      .then((res) => setHealth(res.ok ? 'ok' : 'unreachable'))
      .catch(() => setHealth('unreachable'))
    return () => controller.abort()
  }, [])

  return (
    <main className="flex h-full flex-col items-center justify-center gap-4 bg-page text-fg">
      <h1 className="text-3xl font-semibold">Distributed Flow Lab — Dev</h1>
      <p className="text-fg-muted">Realtime pipeline verification harness.</p>
      <p className="text-sm" data-testid="api-health">
        API status:{' '}
        <span className={health === 'ok' ? 'text-success' : 'text-warning'}>
          {health}
        </span>
      </p>
      <SimulationDevPanel />
    </main>
  )
}
