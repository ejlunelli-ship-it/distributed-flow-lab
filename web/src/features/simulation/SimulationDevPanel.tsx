import { useCallback, useRef, useState } from 'react'
import { useSimulationStore } from '@/state/simulationStore'
import { SimulationHubClient } from '@/realtime/simulationHubClient'
import { createDemoScenario, createSimulation, postLifecycle } from './api'

const statusColor: Record<string, string> = {
  Running: 'text-emerald-400',
  Paused: 'text-amber-400',
  Completed: 'text-sky-400',
  Stopped: 'text-slate-400',
  Failed: 'text-red-400',
}

/**
 * Development panel proving the realtime pipeline end-to-end: creates the
 * demo scenario + simulation over REST, subscribes to the SimulationHub, and
 * renders the live event stream — every row is a real backend event
 * (ADR-006). Superseded by the full canvas + inspector in Sprints 3–5.
 */
export function SimulationDevPanel() {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const hubRef = useRef<SimulationHubClient | null>(null)

  const simulationId = useSimulationStore((s) => s.simulationId)
  const status = useSimulationStore((s) => s.status)
  const currentTick = useSimulationStore((s) => s.currentTick)
  const events = useSimulationStore((s) => s.events)
  const needsResync = useSimulationStore((s) => s.needsResync)

  const runDemo = useCallback(async () => {
    setBusy(true)
    setError(null)
    try {
      const scenario = await createDemoScenario()
      const simulation = await createSimulation(scenario.id, {
        maxTicks: 20,
        tickIntervalMs: 150,
      })

      const store = useSimulationStore.getState()
      store.watch(simulation.id)

      hubRef.current ??= new SimulationHubClient({
        onEvent: (e) => useSimulationStore.getState().applyEvent(e),
        onStateChanged: (s) =>
          useSimulationStore.getState().applyStateChange(s),
      })
      await hubRef.current.start()
      await hubRef.current.subscribe(simulation.id)

      await postLifecycle(simulation.id, 'start')
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e))
    } finally {
      setBusy(false)
    }
  }, [])

  const lifecycle = useCallback(
    async (action: 'pause' | 'resume' | 'stop') => {
      if (!simulationId) return
      try {
        await postLifecycle(simulationId, action)
      } catch (e) {
        setError(e instanceof Error ? e.message : String(e))
      }
    },
    [simulationId],
  )

  return (
    <section className="w-full max-w-2xl rounded-lg border border-slate-700 bg-surface-muted p-4">
      <div className="flex items-center gap-3">
        <button
          type="button"
          onClick={() => void runDemo()}
          disabled={busy}
          className="rounded bg-accent px-3 py-1.5 text-sm font-medium text-white hover:opacity-90 disabled:opacity-50"
        >
          {busy ? 'Starting…' : 'Run demo simulation'}
        </button>
        <button
          type="button"
          onClick={() => void lifecycle('pause')}
          disabled={status !== 'Running'}
          className="rounded border border-slate-600 px-2 py-1 text-sm disabled:opacity-40"
        >
          Pause
        </button>
        <button
          type="button"
          onClick={() => void lifecycle('resume')}
          disabled={status !== 'Paused'}
          className="rounded border border-slate-600 px-2 py-1 text-sm disabled:opacity-40"
        >
          Resume
        </button>
        <button
          type="button"
          onClick={() => void lifecycle('stop')}
          disabled={status !== 'Running' && status !== 'Paused'}
          className="rounded border border-slate-600 px-2 py-1 text-sm disabled:opacity-40"
        >
          Stop
        </button>
      </div>

      {simulationId && (
        <p className="mt-3 text-sm text-slate-400" data-testid="sim-state">
          status:{' '}
          <span className={statusColor[status ?? ''] ?? 'text-slate-300'}>
            {status ?? '—'}
          </span>{' '}
          · tick {currentTick} · {events.length} events
          {needsResync && (
            <span className="ml-2 text-red-400">sequence gap detected!</span>
          )}
        </p>
      )}
      {error && <p className="mt-2 text-sm text-red-400">{error}</p>}

      {events.length > 0 && (
        <ol
          className="mt-3 max-h-64 overflow-y-auto rounded bg-surface p-2 font-mono text-xs"
          data-testid="event-timeline"
        >
          {events.map((e) => (
            <li key={e.eventId} className="flex gap-3 py-0.5 text-slate-300">
              <span className="w-14 text-slate-500">#{e.sequence}</span>
              <span className="w-14 text-slate-500">t{e.tick}</span>
              <span>{e.type}</span>
            </li>
          ))}
        </ol>
      )}
    </section>
  )
}
