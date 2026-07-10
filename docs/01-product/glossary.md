# Distributed Flow Lab — Glossary

This glossary defines the terminology used throughout DFL documentation. It is split into
**distributed-systems concepts** (industry terms the platform teaches) and **DFL domain terms**
(the ubiquitous language from the canon). Definitions are deliberately concise and accurate; the
deeper treatment of each concept lives in the 04-features and 06-learning documentation sets,
linked below. Event names such as `MessagePublished` or `DeadLettered` are canonical and defined
in the [Event Model](../02-architecture/event-model.md).

## Distributed-systems concepts

### Cache
A high-speed store that keeps frequently accessed data close to the requester to avoid repeating
expensive work. In DFL, a `Cache` node models Redis; a lookup that finds data emits `CacheHit`,
a miss emits `CacheMiss`, and an entry removed under pressure or TTL emits `CacheEvicted`.
Caching trades freshness for latency and reduces load on downstream services. See
[Cache feature](../04-features/cache.md).

### Circuit Breaker
A resilience pattern that stops calling a failing dependency to prevent cascading failure. It
moves through states — closed (calls flow), open (calls are short-circuited after a failure
threshold), and half-open (a trial call tests recovery) — emitting `CircuitBreakerOpened`,
`CircuitBreakerHalfOpened`, and `CircuitBreakerClosed`. It gives a struggling service time to
recover instead of being overwhelmed. See [Circuit Breaker feature](../04-features/circuit-breaker.md).

### Consistency (eventual vs strong)
Consistency describes how and when replicas of data agree. **Strong consistency** guarantees that
any read reflects the most recent write, at the cost of latency and availability. **Eventual
consistency** allows replicas to diverge briefly and converge over time, favoring availability
and partition tolerance. DFL visualizes these trade-offs through replication, caching, and
Kafka/consumer-group scenarios. See [Consistency (Distributed Systems primer)](../06-learning/distributed-systems.md).

### Consumer
A participant that receives and processes `Message`s from a `Queue` (RabbitMQ) or `Topic`
partition (Kafka). In DFL a `Consumer` node has configurable processing time and can acknowledge
(`AckReceived`), negatively acknowledge (`MessageNacked`), or fail, which drives retry and
dead-lettering. Consumer throughput relative to producers determines back-pressure.

### Consumer Group
A set of Kafka consumers that cooperate to read a `Topic`, with each `Partition` assigned to
exactly one member of the group so work is divided without duplication. Adding consumers (up to
the partition count) increases parallelism; each group tracks its own `Offset` independently. In
DFL, joining is modeled by `ConsumerRegistered`. See [Kafka feature](../04-features/kafka.md).

### Correlation ID
An identifier shared by all events and messages that belong to the same logical unit of work,
used to group and follow a message's journey. In the DFL event envelope, `correlationId` is the
`messageId` a message-related `SimulationEvent` belongs to, letting the inspector stitch a
message's full path together. It answers "which message is this event about?".

### CQRS (Command Query Responsibility Segregation)
An architectural pattern that separates write operations (commands) from read operations
(queries), often with distinct models and data paths. It allows each side to scale and be
optimized independently and pairs naturally with event-driven designs. In DFL the Application
layer uses MediatR to implement commands/queries, and scenarios visualize the read/write split.
See [CQRS feature](../04-features/cqrs.md).

### DLQ (Dead Letter Queue)
A dedicated queue that receives messages which cannot be delivered or processed successfully —
for example after exhausting retries, expiring, or being rejected. It isolates poison messages so
they neither block the main queue nor are silently lost, enabling later inspection. In DFL a
`DeadLetterQueue` node receives messages via the `DeadLettered` event. See [DLQ feature](../04-features/dlq.md).

### Event Sourcing
A persistence approach that stores state as an append-only log of events rather than as current
mutable rows; current state is derived by replaying (projecting) those events. It provides a
complete audit trail and enables temporal queries and rebuilds. DFL's own event `Timeline` mirrors
this idea, and V3 introduces explicit Event Sourcing scenarios. See [Event Sourcing feature](../04-features/event-sourcing.md).

### Exchange
A RabbitMQ (AMQP) routing component that receives published messages and routes them to bound
`Queue`s according to its type (direct, topic, fanout, headers) and the message's `Routing Key`.
Producers publish to an exchange, never directly to a queue. In DFL, an `Exchange` node performs
this routing, emitting `MessageRouted`. See [RabbitMQ feature](../04-features/rabbitmq.md).

### Idempotency
The property that performing an operation multiple times has the same effect as performing it
once. It is essential in distributed systems because retries and at-least-once delivery can cause
duplicates; idempotent handlers (often keyed by an identifier such as `messageId`) safely absorb
them. DFL uses this to explain why retry and redelivery do not corrupt state.

### Kafka
A distributed, partitioned, append-only log platform for high-throughput event streaming.
Producers append to `Topic`s split into `Partition`s; consumers in a `Consumer Group` read at
their own `Offset`, enabling replay and ordered per-partition consumption. DFL models Kafka with
`Topic`/`Partition` nodes and consumer-group semantics. See [Kafka feature](../04-features/kafka.md).

### Offset
A monotonically increasing position of a record within a Kafka `Partition`. Each `Consumer Group`
commits the offset it has processed, so it can resume after restarts and can replay by rewinding.
Because ordering is guaranteed only within a partition, offsets are per-partition. DFL visualizes
offset progression as consumers advance. See [Kafka feature](../04-features/kafka.md).

### Partition
A Kafka `Topic` is divided into partitions, each an independent ordered log. Partitioning enables
horizontal scale and parallelism (one consumer per partition per group) while guaranteeing
ordering only within a partition, not across the topic. In DFL a `Partition` is a canonical
`NodeType`. See [Kafka feature](../04-features/kafka.md).

### Producer
A participant that creates and sends `Message`s into the system — publishing to an `Exchange`
(RabbitMQ) or appending to a `Topic` (Kafka). In DFL a `Producer` node originates flows and emits
`MessagePublished`. Its publish rate relative to consumer throughput drives queue depth and
back-pressure.

### Queue
A buffer that holds `Message`s in order until a `Consumer` retrieves them, decoupling producers
from consumers in time. In RabbitMQ, queues are bound to an `Exchange` and can have properties
like prefetch, TTL, and a dead-letter target. In DFL a `Queue` node emits `MessageEnqueued` and
`MessageDequeued`, and growing depth illustrates back-pressure. See [RabbitMQ feature](../04-features/rabbitmq.md).

### RabbitMQ
A message broker implementing AMQP, built around `Exchange`s, `Queue`s, bindings, and
`Routing Key`s, with support for dead-letter exchanges (DLX). It excels at flexible routing and
per-message acknowledgement. DFL uses a real RabbitMQ adapter so simulated routing reflects
genuine AMQP behavior. See [RabbitMQ feature](../04-features/rabbitmq.md).

### Redis
An in-memory data store used as a `Cache` and a lightweight pub/sub bus. It offers very low
latency and simple eviction/TTL semantics, making it a common cache and coordination layer. DFL
uses Redis both as a modeled `Cache` node and, at the platform level, as the SignalR backplane for
horizontal scale. See [Redis feature](../04-features/redis.md).

### Retry
Re-attempting a failed operation, typically with a backoff strategy, to recover from transient
faults. Bounded retries prevent infinite loops and, when exhausted, hand a message to a DLQ. In
DFL retries emit `RetryScheduled` and `MessageRetried`, and combine with idempotency to remain
safe. See [Retry feature](../04-features/retry.md).

### Routing Key
A string attribute on a published message that a RabbitMQ `Exchange` uses, together with queue
bindings and the exchange type, to decide which `Queue`s receive the message (e.g.
`order.created`). It is the mechanism behind topic and direct routing. In DFL it appears in the
`payload` of `MessagePublished`/`MessageRouted`. See [RabbitMQ feature](../04-features/rabbitmq.md).

### Saga
A pattern for managing a long-running, multi-step business transaction across services without a
distributed lock. Each step has a compensating action; if a later step fails, previously completed
steps are compensated in reverse to restore consistency. DFL emits `SagaStarted`,
`SagaStepCompleted`, `SagaCompensationTriggered`, and `SagaCompleted`. See [Saga feature](../04-features/saga.md).

### SignalR
The realtime transport (ASP.NET) DFL uses to push authoritative `SimulationEvent`s from server to
client. The `SimulationHub` (at `/hubs/simulation`) groups clients by `simulationId` and delivers
events via `ReceiveSimulationEvent`/`ReceiveSimulationEvents`. Because the client renders only
what it receives, SignalR is what upholds "the backend is the single source of truth". See
[Architecture](../02-architecture/architecture.md).

### Topic
In Kafka, a named, partitioned log to which producers append and from which consumer groups read.
(In RabbitMQ, "topic" also denotes an exchange type that routes by `Routing Key` patterns.) In
DFL, `Topic` is a canonical Kafka `NodeType` composed of one or more `Partition`s. See
[Kafka feature](../04-features/kafka.md).

### Trace ID
An identifier that ties together all spans of a single distributed request as it crosses services,
enabling end-to-end tracing. In the DFL event envelope, `traceId` supports observability
(OpenTelemetry) and lets learners follow one request across many nodes. It answers "which
end-to-end operation does this belong to?" and is broader than `correlationId`.

## DFL domain terms (ubiquitous language)

### Catalog
The library of concept-focused `Scenario` templates (RabbitMQ, Kafka, Saga, CQRS, and more) that
learners browse (`GET /api/v1/catalog`) and instantiate. It is the entry point to structured
learning and, from V3, hosts guided lessons and assessments. See [Distributed Systems primer](../06-learning/distributed-systems.md).

### Edge
A directed connection between two `Node`s (e.g. `Producer→Exchange`, `Service→Database`), carrying
a label and type-specific config. Edges define the topology along which `Message` tokens animate.

### Fault Injection
The deliberate introduction of failure, latency, or network partition into a running `Simulation`
to teach failure behavior safely. Injected via `POST /api/v1/simulations/{id}/faults`, it produces
events such as `FaultInjected`, `LatencyInjected`, `PartitionCreated`, `PartitionHealed`,
`NodeFailed`, and `NodeRecovered`. See [Fault injection (Common Mistakes)](../06-learning/common-mistakes.md).

### Message
A unit of work or data flowing through the system, carrying `messageId`, `correlationId`,
`traceId`, and `payload`. Messages are what animate along edges; their lifecycle (published →
routed → enqueued → processed → acked, or retried/dead-lettered) is expressed entirely through
`SimulationEvent`s.

### Node
A participant in the architecture, typed by the canonical `NodeType` enum (`Producer`, `Consumer`,
`Service`, `ApiGateway`, `LoadBalancer`, `Exchange`, `Queue`, `Topic`, `Partition`, `Broker`,
`Database`, `Cache`, `DeadLetterQueue`, `Client`). Each node has a label, canvas position, and
type-specific configuration.

### Scenario
A saved architecture template: a topology of `Node`s and `Edge`s plus configuration, reusable as a
blueprint and tagged with a `conceptTag`. Scenarios are authored on the canvas and managed via
`/api/v1/scenarios`.

### Simulation
A running (or completed) execution instance of a `Scenario`. It has a lifecycle
(`Draft|Running|Paused|Completed|Stopped|Failed`), a current `Tick`, and a `Timeline` of events,
and is controlled via the simulation REST endpoints and the `SimulationHub`.

### SimulationEvent
The domain event emitted by the engine during a `Simulation` — the single unit of truth for
animation. Each is carried in the canonical envelope (with `sequence`, `tick`, `type`, source/target
node ids, `correlationId`, `traceId`, `payload`). The frontend renders only these; it never invents
state. See [Event Model](../02-architecture/event-model.md).

### Tick
The discrete logical-clock unit of the simulation engine. Ticks make simulations deterministic and
reproducible; each advance emits `TickAdvanced`. Wall-clock time (`occurredAt`) is separate from
this logical clock.

### Timeline
The ordered, replayable sequence of `SimulationEvent`s for a `Simulation`, ordered by monotonic
`sequence`. It powers the event inspector and, from V2, timeline scrubbing and deterministic
replay via `GET /api/v1/simulations/{id}/events?fromSequence=`.

## Related documents

- [Vision](./vision.md)
- [Product Requirements Document](./prd.md)
- [Personas](./personas.md)
- [Roadmap](./roadmap.md)
- [Backlog](./backlog.md)
- [Architecture](../02-architecture/architecture.md)
- [Event Model](../02-architecture/event-model.md)
- [Feature simulations (documentation index)](../README.md)
- [Distributed Systems primer](../06-learning/distributed-systems.md)
