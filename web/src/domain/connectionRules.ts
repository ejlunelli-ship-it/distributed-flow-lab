import type { NodeType } from './nodeType'

/**
 * Connection legality rules for the canvas (Epic 1, "Connection validation by node-type rules").
 *
 * There is no single canonical connection matrix in the documentation set — only the illustrative
 * rule that `Consumer → Producer` is illegal (backlog Epic 1). Because DFL teaches *correct*
 * distributed topologies, we encode a curated, directional source→target matrix rather than
 * accepting any wiring. The rationale, the full matrix, and its extensibility are recorded in
 * ADR-016. Keep this in sync with the backend when server-side scenario validation is added.
 *
 * A directed edge `source → target` is legal iff `target`'s type is in `ALLOWED_TARGETS[source]`.
 * Self-loops and duplicate edges are rejected by the caller (`canvasStore`), which has node
 * identity; this module reasons purely about `NodeType`s so it stays a pure, unit-testable rule.
 */
const ALLOWED_TARGETS: Record<NodeType, readonly NodeType[]> = {
  // Actors originate flow.
  Producer: [
    'Exchange',
    'Queue',
    'Topic',
    'Broker',
    'Service',
    'ApiGateway',
    'LoadBalancer',
  ],
  Client: ['ApiGateway', 'LoadBalancer', 'Service'],
  // Compute can call downstream services/storage and publish to messaging infrastructure.
  Service: [
    'Service',
    'Database',
    'Cache',
    'Exchange',
    'Queue',
    'Topic',
    'Broker',
    'ApiGateway',
    'LoadBalancer',
  ],
  // Consumers are sinks: they process messages and may persist or invoke a service — never
  // publish back to a Producer (the canonical illegal example).
  Consumer: ['Database', 'Cache', 'Service'],
  // Networking edges route/fan out to compute (and to each other in a gateway↔balancer tier).
  ApiGateway: ['Service', 'LoadBalancer'],
  LoadBalancer: ['Service', 'ApiGateway'],
  // RabbitMQ: exchanges route to queues (and other exchanges); queues deliver / dead-letter.
  Exchange: ['Queue', 'DeadLetterQueue', 'Exchange'],
  Queue: ['Consumer', 'Service', 'DeadLetterQueue'],
  DeadLetterQueue: ['Consumer', 'Service'],
  // Kafka: brokers host topics; topics fan into partitions; both deliver to consumers.
  Broker: ['Topic'],
  Topic: ['Partition', 'Consumer'],
  Partition: ['Consumer'],
  // Storage is terminal in the message/request graph.
  Database: [],
  Cache: [],
}

export interface ConnectionValidation {
  valid: boolean
  /** Human, educational reason a connection was rejected — surfaced inline in the UI. */
  reason?: string
}

/**
 * Validates a `source → target` connection by `NodeType`. Returns a legality verdict plus an
 * educational reason when rejected (shown inline on the canvas per the Epic 1 acceptance
 * criteria). Node-identity checks (self-loop, duplicate) are handled by the caller.
 */
export function validateConnection(
  source: NodeType,
  target: NodeType,
): ConnectionValidation {
  const allowed = ALLOWED_TARGETS[source]
  if (allowed.includes(target)) {
    return { valid: true }
  }
  if (allowed.length === 0) {
    return {
      valid: false,
      reason: `A ${source} is a terminal node and cannot originate connections.`,
    }
  }
  return {
    valid: false,
    reason: `A ${source} cannot connect to a ${target}.`,
  }
}

/** Whether `source` can legally originate an edge to `target` (identity checks excluded). */
export function canConnect(source: NodeType, target: NodeType): boolean {
  return validateConnection(source, target).valid
}
