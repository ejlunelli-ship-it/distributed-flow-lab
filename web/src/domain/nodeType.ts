/**
 * Canonical NodeType vocabulary — mirrors DistributedFlowLab.Domain `NodeType`
 * (.docs/02-architecture/data-model.md §3.1). Keep in exact sync with the backend enum.
 */
export const NODE_TYPES = [
  'Producer',
  'Consumer',
  'Service',
  'ApiGateway',
  'LoadBalancer',
  'Exchange',
  'Queue',
  'Topic',
  'Partition',
  'Broker',
  'Database',
  'Cache',
  'DeadLetterQueue',
  'Client',
] as const

export type NodeType = (typeof NODE_TYPES)[number]
