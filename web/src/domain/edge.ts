import type { NodeConfigValue } from './node'

/**
 * The `data` payload of a React Flow edge (.docs/02-architecture/data-model.md §2.3). An `Edge`
 * is a directed connection carrying an optional `label` (e.g. a routing key / binding) and a
 * flat `config` bag (e.g. `routingKey`, weight, latency). Like nodes, the `canvasStore` owns it.
 */
export interface FlowEdgeData extends Record<string, unknown> {
  label: string
  config: Record<string, NodeConfigValue>
}
