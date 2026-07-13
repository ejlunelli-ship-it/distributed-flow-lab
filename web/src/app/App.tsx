import { useEffect, useState } from 'react'
import { SimulationDevPanel } from '@/features/simulation/SimulationDevPanel'

type ApiHealth = 'checking' | 'ok' | 'unreachable'

/**
 * Application shell (scaffolding). The canvas / simulation / inspector / catalog
 * features (.docs/03-ui) mount here from Sprint 3 onward. For now it verifies the
 * SPA can reach the API through the dev proxy (/api → api container).
 */
export function App() {
  const [health, setHealth] = useState<ApiHealth>('checking')

  useEffect(() => {
    const controller = new AbortController()
    fetch('/health', { signal: controller.signal })
      .then((res) => setHealth(res.ok ? 'ok' : 'unreachable'))
      .catch(() => setHealth('unreachable'))
    return () => controller.abort()
  }, [])

  return (
    <main className="flex h-full flex-col items-center justify-center gap-4 bg-surface text-slate-100">
      <h1 className="text-3xl font-semibold">Distributed Flow Lab</h1>
      <p className="text-slate-400">
        Learn distributed systems through interactive visual simulations.
      </p>
      <p className="text-sm" data-testid="api-health">
        API status:{' '}
        <span
          className={health === 'ok' ? 'text-emerald-400' : 'text-amber-400'}
        >
          {health}
        </span>
      </p>
      <SimulationDevPanel />
    </main>
  )
}
