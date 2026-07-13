/**
 * Simulation status and realtime state DTO — mirrors the backend
 * `SimulationStatus` enum (.docs/02-architecture/data-model.md §3.2) and the
 * `SimulationStateDto` pushed via `SimulationStateChanged`.
 */
export const SIMULATION_STATUSES = [
  'Draft',
  'Running',
  'Paused',
  'Completed',
  'Stopped',
  'Failed',
] as const

export type SimulationStatus = (typeof SIMULATION_STATUSES)[number]

/** Payload of the `SimulationStateChanged` push (websocket-events.md §3). */
export interface SimulationStateDto {
  simulationId: string
  status: SimulationStatus
  currentTick: number
  updatedAt: string
}

/** Simulation resource returned by /api/v1/simulations (api-contracts.md §4). */
export interface SimulationDto {
  id: string
  scenarioId: string
  status: SimulationStatus
  currentTick: number
  maxTicks: number
  tickIntervalMs: number
  createdAt: string
  startedAt: string | null
  endedAt: string | null
}
