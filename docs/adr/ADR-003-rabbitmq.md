# ADR-003: Real RabbitMQ as a messaging adapter for fidelity

- **Status:** Accepted
- **Date:** 2026-07-07

## Context

DFL's product thesis is that it must **teach** distributed systems, not merely draw a
cartoon of them (canon §0). A stated architectural goal is "realistic broker behavior":
real infrastructure adapters back the abstract node types so behavior reflects production
semantics (`../02-architecture/architecture.md` §2). For messaging, RabbitMQ is DFL's
canonical example of a **smart-broker / dumb-consumer** system — it teaches how a
`Message` is *routed*, not merely moved, from a `Producer` through an `Exchange` to bound
`Queue`s, and how acknowledgement, negative acknowledgement, TTL, and dead-lettering
govern reliable delivery (see `../04-features/rabbitmq.md`).

The behaviors DFL must reproduce truthfully include:

- Exchange types and routing: `direct`, `topic`, `fanout`, `headers`, and the exact
  fan-out of a routing key against a set of bindings.
- Competing consumers, `prefetch` fairness, and per-queue delivery.
- Acknowledgement contract: `AckReceived`, `MessageNacked`, requeue vs. `requeue=false`.
- Dead-lettering via a Dead Letter Exchange (`x-dead-letter-exchange`) to a
  `DeadLetterQueue`, plus TTL expiry and max-length overflow.

These are subtle and easy to get *almost* right. A caricature that gets the edge cases
wrong would teach students the wrong mental model — the opposite of the product's purpose.

RabbitMQ is one of several **adapters** behind the messaging port (canon §3): Kafka backs
`Topic`/`Partition` nodes and Redis backs `Cache` nodes and the SignalR backplane (canon
§2). This ADR concerns whether the RabbitMQ concepts are backed by a **real broker** or a
fake, and applies the same reasoning by analogy to Kafka and Redis.

## Decision

We back RabbitMQ node types (`Exchange`, `Queue`, `DeadLetterQueue`, and the
`Producer`/`Consumer` interactions with them) with a **real RabbitMQ broker**, integrated
through a messaging-adapter **port** defined in the Application layer and implemented in
Infrastructure (canon §3). RabbitMQ runs as the `rabbitmq` container in Docker Compose
(canon §2).

The Simulation Engine drives real AMQP operations — declaring per-simulation exchanges and
queues (isolated by `simulationId`, see `../02-architecture/architecture.md` §8), publishing
with routing keys, binding queues, consuming, acking/nacking, and configuring
`x-dead-letter-exchange`, `x-message-ttl`, and `x-max-length`. The **observed** broker
behavior is what the engine translates into canonical `SimulationEvent`s
(`MessagePublished`, `MessageRouted`, `MessageEnqueued`, `MessageDequeued`, `AckReceived`,
`MessageNacked`, `MessageExpired`, `MessageDropped`, `DeadLettered` — canon §7). The
frontend animates those events and never fabricates routing decisions.

Kafka and Redis are treated the same way: **real brokers behind adapter ports**, so their
distinctive semantics (partitions/offsets/consumer groups; cache hit/miss/eviction) are
truthful. Integration tests exercise the adapters against real brokers via **Testcontainers**
(canon §2).

## Alternatives

### Pure in-memory fake broker
Model AMQP routing in C# with no external broker. **Rejected:** it would force us to
re-implement RabbitMQ's routing, prefetch, dead-lettering, TTL, and overflow semantics by
hand — and any divergence teaches students a subtly wrong model, defeating the product's
educational purpose. It also gives us zero confidence that the adapter matches real AMQP.
(An in-memory *deterministic* mode remains valuable for fast unit tests of the engine's
event translation, but it is not the source of truth for behavior.)

### Kafka-only (one broker for everything)
Standardize on Kafka and emulate queue/exchange semantics on top of it. **Rejected:** Kafka
and RabbitMQ embody genuinely different models — log/partition/offset/consumer-group versus
exchange/binding/queue/ack/DLX. A core learning objective is to **contrast** them
(`../01-product/vision.md`, `../04-features/rabbitmq.md`). Emulating routing, per-message
ack, and DLX on a log would be both inaccurate and misleading, and would erase exactly the
distinction we want students to internalize.

### No real broker at all (animation-only)
Script the animations directly with no broker and no real events. **Rejected outright:** it
violates the first golden rule — animations must be driven by real backend events, and the
frontend must never invent state (canon §1). It would make DFL the "toy that looks like a
broker" the vision explicitly rejects.

## Consequences

### Positive
- Simulated messaging behavior matches production RabbitMQ semantics, including the edge
  cases (routing fan-out, DLX, TTL, overflow) that are the real curriculum.
- The same port-and-adapter pattern generalizes cleanly to Kafka and Redis and to future
  brokers (NATS, SQS) without touching Domain, Application, or the event envelope
  (`../02-architecture/architecture.md` §9).
- Testcontainers integration tests run the adapters against the real broker, giving high
  confidence the events we emit reflect reality.
- Students who graduate from DFL to production carry an accurate mental model.

### Negative
- Operational weight: local dev and CI must run RabbitMQ (and Kafka, Redis). Mitigation:
  Docker Compose one-command bring-up (ADR-005) and Testcontainers for CI.
- Determinism: real brokers introduce timing/ordering nondeterminism relative to the
  engine's logical `tick`. Mitigation: the engine owns the monotonic `sequence` and maps
  observed broker outcomes onto ticks; an in-memory mode is available for deterministic
  unit tests.
- Per-simulation resource creation (exchanges/queues per `simulationId`) must be cleaned up
  to avoid broker resource leaks. Mitigation: resources are namespaced by `simulationId`
  and torn down on `SimulationStopped`/`SimulationCompleted`, with bounded resource limits.

## Related documents

- [Architecture](../02-architecture/architecture.md)
- [RabbitMQ Feature](../04-features/rabbitmq.md)
- [Event Model](../02-architecture/event-model.md)
- [RabbitMQ Flow](../diagrams/rabbitmq-flow.md)
- [Kafka Flow](../diagrams/kafka-flow.md)
- [ADR Index](./README.md)
