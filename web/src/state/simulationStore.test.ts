import { describe, it, expect, beforeEach } from 'vitest'
import { useSimulationStore } from './simulationStore'
import type { SimulationEvent } from '@/domain'

const SIM_ID = 'sim-1'

function evt(
  sequence: number,
  tick = sequence,
  type = 'TickAdvanced',
): SimulationEvent {
  return {
    eventId: `e-${sequence}`,
    simulationId: SIM_ID,
    sequence,
    tick,
    occurredAt: '2026-07-10T12:00:00Z',
    type: type as SimulationEvent['type'],
    sourceNodeId: 'engine',
    targetNodeId: null,
    correlationId: `c-${sequence}`,
    traceId: `t-${sequence}`,
    payload: {},
  }
}

describe('simulationStore', () => {
  beforeEach(() => {
    useSimulationStore.getState().reset()
    useSimulationStore.getState().watch(SIM_ID)
  })

  it('applies contiguous events in sequence order', () => {
    const store = useSimulationStore.getState()
    store.applyEvent(evt(0))
    store.applyEvent(evt(1))
    store.applyEvent(evt(2))

    const state = useSimulationStore.getState()
    expect(state.events.map((e) => e.sequence)).toEqual([0, 1, 2])
    expect(state.lastAppliedSequence).toBe(2)
    expect(state.needsResync).toBe(false)
  })

  it('ignores duplicate deliveries', () => {
    const store = useSimulationStore.getState()
    store.applyEvent(evt(0))
    store.applyEvent(evt(0))
    store.applyEvent(evt(1))
    store.applyEvent(evt(1))

    expect(useSimulationStore.getState().events).toHaveLength(2)
  })

  it('buffers out-of-order events and flags a resync on gap', () => {
    const store = useSimulationStore.getState()
    store.applyEvent(evt(0))
    store.applyEvent(evt(3)) // gap: 1 and 2 missing

    const state = useSimulationStore.getState()
    expect(state.events.map((e) => e.sequence)).toEqual([0])
    expect(state.needsResync).toBe(true)
    expect(state.pendingBySequence.has(3)).toBe(true)
  })

  it('drains the buffer once the gap is backfilled', () => {
    const store = useSimulationStore.getState()
    store.applyEvent(evt(0))
    store.applyEvent(evt(3))
    store.applyEvent(evt(4))

    // Backfill arrives via GET /events?fromSequence=1
    store.applyEvents([evt(1), evt(2)])

    const state = useSimulationStore.getState()
    expect(state.events.map((e) => e.sequence)).toEqual([0, 1, 2, 3, 4])
    expect(state.lastAppliedSequence).toBe(4)
    expect(state.needsResync).toBe(false)
    expect(state.pendingBySequence.size).toBe(0)
  })

  it('ignores events from other simulations', () => {
    const store = useSimulationStore.getState()
    store.applyEvent({ ...evt(0), simulationId: 'other-sim' })

    expect(useSimulationStore.getState().events).toHaveLength(0)
  })

  it('folds SimulationStateChanged into status and tick', () => {
    const store = useSimulationStore.getState()
    store.applyStateChange({
      simulationId: SIM_ID,
      status: 'Running',
      currentTick: 7,
      updatedAt: '2026-07-10T12:00:01Z',
    })

    const state = useSimulationStore.getState()
    expect(state.status).toBe('Running')
    expect(state.currentTick).toBe(7)
  })

  it('watch resets state for a new simulation', () => {
    const store = useSimulationStore.getState()
    store.applyEvent(evt(0))
    store.watch('sim-2')

    const state = useSimulationStore.getState()
    expect(state.simulationId).toBe('sim-2')
    expect(state.events).toHaveLength(0)
    expect(state.lastAppliedSequence).toBe(-1)
  })
})
