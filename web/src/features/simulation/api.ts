import type { SimulationDto } from '@/domain'

/**
 * Thin REST helpers for the simulation endpoints (api-contracts.md §3–§4).
 * Requests go through the dev-server / Nginx proxy on the same origin.
 */

async function json<T>(response: Response): Promise<T> {
  if (!response.ok) {
    throw new Error(`${response.status} ${response.statusText}`)
  }
  return (await response.json()) as T
}

/** Creates the canonical demo topology: Producer → Exchange → Queue → Consumer. */
export async function createDemoScenario(): Promise<{ id: string }> {
  return json(
    await fetch('/api/v1/scenarios', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        name: `Demo pipeline ${new Date().toISOString()}`,
        description: 'Producer → Exchange → Queue → Consumer',
        conceptTag: 'RabbitMQ',
        nodes: [
          {
            id: 'node-producer-1',
            type: 'Producer',
            label: 'Order API',
            position: { x: 40, y: 120 },
            config: {},
          },
          {
            id: 'node-exchange-1',
            type: 'Exchange',
            label: 'orders.ex',
            position: { x: 260, y: 120 },
            config: {},
          },
          {
            id: 'node-queue-1',
            type: 'Queue',
            label: 'orders.q',
            position: { x: 480, y: 120 },
            config: {},
          },
          {
            id: 'node-consumer-1',
            type: 'Consumer',
            label: 'Billing',
            position: { x: 700, y: 120 },
            config: {},
          },
        ],
        edges: [
          {
            id: 'edge-1',
            sourceNodeId: 'node-producer-1',
            targetNodeId: 'node-exchange-1',
            label: 'publish',
            config: {},
          },
          {
            id: 'edge-2',
            sourceNodeId: 'node-exchange-1',
            targetNodeId: 'node-queue-1',
            label: 'order.created',
            config: {},
          },
          {
            id: 'edge-3',
            sourceNodeId: 'node-queue-1',
            targetNodeId: 'node-consumer-1',
            label: 'deliver',
            config: {},
          },
        ],
      }),
    }),
  )
}

export async function createSimulation(
  scenarioId: string,
  options: { maxTicks: number; tickIntervalMs: number },
): Promise<SimulationDto> {
  return json(
    await fetch('/api/v1/simulations', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ scenarioId, options }),
    }),
  )
}

export async function postLifecycle(
  simulationId: string,
  action: 'start' | 'pause' | 'resume' | 'stop',
): Promise<SimulationDto> {
  return json(
    await fetch(`/api/v1/simulations/${simulationId}/${action}`, {
      method: 'POST',
    }),
  )
}
