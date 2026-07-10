/**
 * Canonical SimulationEvent envelope and Event Catalog — mirrors
 * .docs/02-architecture/event-model.md. The frontend renders these events and
 * never invents domain state (ADR-006). Keep in exact sync with the backend.
 */

/** Every event name in the canonical Event Catalog (event-model.md §3). */
export const SIMULATION_EVENT_TYPES = [
  // Lifecycle
  'SimulationStarted',
  'SimulationPaused',
  'SimulationResumed',
  'SimulationStopped',
  'SimulationCompleted',
  'TickAdvanced',
  // Node
  'NodeActivated',
  'NodeStateChanged',
  'NodeFailed',
  'NodeRecovered',
  'ConsumerRegistered',
  // Messaging
  'MessagePublished',
  'MessageRouted',
  'MessageEnqueued',
  'MessageDequeued',
  'MessageReceived',
  'MessageProcessed',
  'AckReceived',
  'MessageNacked',
  'RetryScheduled',
  'MessageRetried',
  'DeadLettered',
  'MessageExpired',
  'MessageDropped',
  // HTTP / RPC
  'HttpRequestStarted',
  'HttpResponseReceived',
  'HttpRequestFailed',
  'HttpRequestTimedOut',
  'GrpcCallStarted',
  'GrpcCallCompleted',
  // Resilience / patterns
  'CircuitBreakerOpened',
  'CircuitBreakerHalfOpened',
  'CircuitBreakerClosed',
  'SagaStarted',
  'SagaStepCompleted',
  'SagaCompensationTriggered',
  'SagaCompleted',
  'CacheHit',
  'CacheMiss',
  'CacheEvicted',
  // Fault injection
  'FaultInjected',
  'LatencyInjected',
  'PartitionCreated',
  'PartitionHealed',
] as const

export type SimulationEventType = (typeof SIMULATION_EVENT_TYPES)[number]

/** The canonical event envelope transported over SignalR (camelCase on the wire). */
export interface SimulationEvent {
  eventId: string
  simulationId: string
  /** Monotonic per simulation — drives ordering and gap detection. */
  sequence: number
  /** Engine logical clock at emission time. */
  tick: number
  occurredAt: string
  type: SimulationEventType
  sourceNodeId: string
  targetNodeId: string | null
  correlationId: string
  traceId: string
  payload: Record<string, unknown>
}

/**
 * Frontend-only presentation events. Derived on the client to describe the
 * *rendering* of a backend event; never persisted, streamed, or treated as
 * domain truth (event-model.md §5).
 */
export interface AnimationStarted {
  readonly kind: 'AnimationStarted'
  readonly eventId: string
  readonly sourceNodeId: string
  readonly targetNodeId: string | null
}

export interface AnimationFinished {
  readonly kind: 'AnimationFinished'
  readonly eventId: string
}
