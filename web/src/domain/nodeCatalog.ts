import type { NodeType } from './nodeType'
import type { NodeConfig, NodeConfigField } from './node'

/**
 * The node catalog — the single declarative description of every canonical `NodeType`: which
 * palette family it belongs to, a short educational blurb, and its configurable fields. The
 * palette, the default `config` of a freshly-dropped node, and the inspector's config form are
 * ALL derived from this table, so adding or changing a `NodeType` is a data edit here — never a
 * `switch` scattered across components (OCP, coding-standards §3.4 applied on the client).
 *
 * Palette families follow the `NodeType` groupings in data-model.md §3.1 (Actors/Compute,
 * Networking, Messaging = RabbitMQ + Kafka, Storage/Data). Per-type accent colors are not stored
 * here: they derive mechanically from the design tokens as `var(--node-<type>)` (design-system.md
 * §2.2) — see `features/canvas/nodeCatalog` accessor.
 */
export type PaletteGroup = 'Compute' | 'Networking' | 'Messaging' | 'Data'

/** Order in which palette groups are rendered top-to-bottom. */
export const PALETTE_GROUPS: readonly PaletteGroup[] = [
  'Compute',
  'Networking',
  'Messaging',
  'Data',
]

export interface NodeTypeMeta {
  type: NodeType
  group: PaletteGroup
  description: string
  configFields: readonly NodeConfigField[]
}

export const NODE_CATALOG: Record<NodeType, NodeTypeMeta> = {
  Producer: {
    type: 'Producer',
    group: 'Compute',
    description: 'Originates messages onto a broker, exchange, or service.',
    configFields: [
      {
        key: 'publishRatePerTick',
        label: 'Publish rate / tick',
        kind: 'number',
        defaultValue: 1,
        min: 0,
        max: 100,
      },
      {
        key: 'routingKey',
        label: 'Routing key',
        kind: 'text',
        defaultValue: 'order.created',
      },
    ],
  },
  Consumer: {
    type: 'Consumer',
    group: 'Compute',
    description: 'Receives and processes messages; a message sink.',
    configFields: [
      {
        key: 'prefetch',
        label: 'Prefetch',
        kind: 'number',
        defaultValue: 10,
        min: 0,
        max: 1000,
      },
      {
        key: 'ackMode',
        label: 'Ack mode',
        kind: 'select',
        defaultValue: 'manual',
        options: ['auto', 'manual'],
      },
    ],
  },
  Service: {
    type: 'Service',
    group: 'Compute',
    description:
      'A compute unit that handles requests and may call downstream dependencies.',
    configFields: [
      {
        key: 'processingMs',
        label: 'Processing (ms)',
        kind: 'number',
        defaultValue: 20,
        min: 0,
        max: 60000,
      },
      {
        key: 'failureRate',
        label: 'Failure rate',
        kind: 'number',
        defaultValue: 0,
        min: 0,
        max: 1,
        help: 'Fraction 0–1 of requests that fail.',
      },
    ],
  },
  Client: {
    type: 'Client',
    group: 'Compute',
    description: 'An external caller issuing requests into the system.',
    configFields: [
      {
        key: 'requestRatePerTick',
        label: 'Request rate / tick',
        kind: 'number',
        defaultValue: 1,
        min: 0,
        max: 100,
      },
    ],
  },
  ApiGateway: {
    type: 'ApiGateway',
    group: 'Networking',
    description: 'Single entry point routing requests to downstream services.',
    configFields: [
      {
        key: 'timeoutMs',
        label: 'Timeout (ms)',
        kind: 'number',
        defaultValue: 5000,
        min: 0,
        max: 120000,
      },
    ],
  },
  LoadBalancer: {
    type: 'LoadBalancer',
    group: 'Networking',
    description: 'Distributes requests across backend instances.',
    configFields: [
      {
        key: 'strategy',
        label: 'Strategy',
        kind: 'select',
        defaultValue: 'round-robin',
        options: ['round-robin', 'random', 'least-connections'],
      },
    ],
  },
  Exchange: {
    type: 'Exchange',
    group: 'Messaging',
    description: 'RabbitMQ exchange routing messages to queues by binding.',
    configFields: [
      {
        key: 'exchangeType',
        label: 'Exchange type',
        kind: 'select',
        defaultValue: 'topic',
        options: ['direct', 'topic', 'fanout', 'headers'],
      },
    ],
  },
  Queue: {
    type: 'Queue',
    group: 'Messaging',
    description: 'RabbitMQ queue buffering messages for consumers.',
    configFields: [
      {
        key: 'maxLength',
        label: 'Max length',
        kind: 'number',
        defaultValue: 1000,
        min: 0,
      },
      {
        key: 'ttlMs',
        label: 'Message TTL (ms)',
        kind: 'number',
        defaultValue: 30000,
        min: 0,
      },
      {
        key: 'prefetch',
        label: 'Prefetch',
        kind: 'number',
        defaultValue: 10,
        min: 0,
      },
      {
        key: 'deadLetterRoutingKey',
        label: 'Dead-letter routing key',
        kind: 'text',
        defaultValue: '',
      },
    ],
  },
  DeadLetterQueue: {
    type: 'DeadLetterQueue',
    group: 'Messaging',
    description: 'Receives messages that could not be delivered or processed.',
    configFields: [
      {
        key: 'maxLength',
        label: 'Max length',
        kind: 'number',
        defaultValue: 1000,
        min: 0,
      },
    ],
  },
  Topic: {
    type: 'Topic',
    group: 'Messaging',
    description: 'Kafka topic; an append-only log split into partitions.',
    configFields: [
      {
        key: 'partitions',
        label: 'Partitions',
        kind: 'number',
        defaultValue: 3,
        min: 1,
        max: 100,
      },
      {
        key: 'retentionMs',
        label: 'Retention (ms)',
        kind: 'number',
        defaultValue: 604800000,
        min: 0,
      },
    ],
  },
  Partition: {
    type: 'Partition',
    group: 'Messaging',
    description: 'A single ordered partition of a Kafka topic.',
    configFields: [
      {
        key: 'index',
        label: 'Partition index',
        kind: 'number',
        defaultValue: 0,
        min: 0,
      },
    ],
  },
  Broker: {
    type: 'Broker',
    group: 'Messaging',
    description: 'A Kafka broker hosting topics and partitions.',
    configFields: [],
  },
  Database: {
    type: 'Database',
    group: 'Data',
    description: 'Durable storage read from and written to by services.',
    configFields: [
      {
        key: 'readLatencyMs',
        label: 'Read latency (ms)',
        kind: 'number',
        defaultValue: 10,
        min: 0,
      },
      {
        key: 'writeLatencyMs',
        label: 'Write latency (ms)',
        kind: 'number',
        defaultValue: 15,
        min: 0,
      },
    ],
  },
  Cache: {
    type: 'Cache',
    group: 'Data',
    description: 'In-memory cache fronting a slower data store.',
    configFields: [
      {
        key: 'policy',
        label: 'Eviction policy',
        kind: 'select',
        defaultValue: 'LRU',
        options: ['LRU', 'LFU', 'FIFO'],
      },
      {
        key: 'maxEntries',
        label: 'Max entries',
        kind: 'number',
        defaultValue: 10000,
        min: 1,
      },
      {
        key: 'ttlMs',
        label: 'TTL (ms)',
        kind: 'number',
        defaultValue: 60000,
        min: 0,
      },
    ],
  },
}

/** Builds the default `config` for a node of `type` from its catalog field descriptors. */
export function defaultConfigFor(type: NodeType): NodeConfig {
  const config: NodeConfig = {}
  for (const field of NODE_CATALOG[type].configFields) {
    config[field.key] = field.defaultValue
  }
  return config
}

/** The `NodeType`s belonging to a palette group, in catalog order. */
export function nodeTypesInGroup(group: PaletteGroup): NodeType[] {
  return Object.values(NODE_CATALOG)
    .filter((meta) => meta.group === group)
    .map((meta) => meta.type)
}
