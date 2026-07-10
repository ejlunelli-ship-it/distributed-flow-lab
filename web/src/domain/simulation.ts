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

export interface SimulationStateDto {
  simulationId: string
  scenarioId: string
  status: SimulationStatus
  currentTick: number
}
