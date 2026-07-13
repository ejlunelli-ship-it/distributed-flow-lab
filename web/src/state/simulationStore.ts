import { create } from 'zustand'
import type {
  SimulationEvent,
  SimulationStateDto,
  SimulationStatus,
} from '@/domain'

/**
 * Realtime simulation state — a fold over the authoritative backend event
 * stream (ADR-006). Ordering rules follow websocket-events.md §5:
 *
 * - events are applied strictly in `sequence` order;
 * - duplicates (sequence ≤ last applied) are ignored;
 * - a jump beyond `lastAppliedSequence + 1` is a GAP: the event is buffered,
 *   `needsResync` is raised, and the missing range must be backfilled via
 *   GET /api/v1/simulations/{id}/events?fromSequence= — the client never
 *   invents the missing state.
 *
 * `status`/`currentTick` come from `SimulationStateChanged` pushes (and from
 * applied `TickAdvanced` events), never from guesses.
 */
interface SimulationRealtimeState {
  simulationId: string | null
  status: SimulationStatus | null
  currentTick: number
  /** Applied events, contiguous and ordered by sequence. */
  events: SimulationEvent[]
  /** Highest contiguous sequence applied; -1 before the first event. */
  lastAppliedSequence: number
  /** Out-of-order events parked until the gap before them is filled. */
  pendingBySequence: Map<number, SimulationEvent>
  /** True when a gap was detected and a backfill is required. */
  needsResync: boolean

  /** Resets the store to observe one simulation. */
  watch: (simulationId: string) => void
  /** Applies a live event (or a backfilled one) honoring sequence order. */
  applyEvent: (event: SimulationEvent) => void
  /** Applies a batch (e.g. REST backfill), in ascending sequence order. */
  applyEvents: (events: SimulationEvent[]) => void
  /** Folds a SimulationStateChanged push into the store. */
  applyStateChange: (state: SimulationStateDto) => void
  reset: () => void
}

const initial = {
  simulationId: null as string | null,
  status: null as SimulationStatus | null,
  currentTick: 0,
  events: [] as SimulationEvent[],
  lastAppliedSequence: -1,
  pendingBySequence: new Map<number, SimulationEvent>(),
  needsResync: false,
}

export const useSimulationStore = create<SimulationRealtimeState>(
  (set, get) => ({
    ...initial,

    watch: (simulationId) => set({ ...initial, simulationId }),

    applyEvent: (event) => {
      const { simulationId, lastAppliedSequence, events, pendingBySequence } =
        get()
      if (simulationId !== null && event.simulationId !== simulationId) {
        return // not the simulation we observe
      }
      if (event.sequence <= lastAppliedSequence) {
        return // duplicate delivery
      }

      if (event.sequence > lastAppliedSequence + 1) {
        // Gap: park the event and request a resync — never fabricate state.
        const pending = new Map(pendingBySequence)
        pending.set(event.sequence, event)
        set({ pendingBySequence: pending, needsResync: true })
        return
      }

      // Contiguous: apply, then drain any parked events that became contiguous.
      const applied = [...events, event]
      let next = event.sequence + 1
      const pending = new Map(pendingBySequence)
      while (pending.has(next)) {
        applied.push(pending.get(next)!)
        pending.delete(next)
        next += 1
      }

      set({
        events: applied,
        lastAppliedSequence: next - 1,
        pendingBySequence: pending,
        needsResync: pending.size > 0,
        currentTick: Math.max(
          get().currentTick,
          applied[applied.length - 1]!.tick,
        ),
      })
    },

    applyEvents: (batch) => {
      const sorted = [...batch].sort((a, b) => a.sequence - b.sequence)
      for (const event of sorted) {
        get().applyEvent(event)
      }
    },

    applyStateChange: (state) => {
      if (
        get().simulationId !== null &&
        state.simulationId !== get().simulationId
      ) {
        return
      }
      set({
        status: state.status,
        currentTick: Math.max(get().currentTick, state.currentTick),
      })
    },

    reset: () => set({ ...initial }),
  }),
)
