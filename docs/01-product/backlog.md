# Distributed Flow Lab — Product Backlog

This backlog decomposes the [Roadmap](./roadmap.md) and [PRD](./prd.md) into **Epics → Features →
Tasks**. It is the working queue for delivery. Priorities are **P0** (must-have for the epic's
target phase / blocking), **P1** (important, near-term), **P2** (valuable, deferrable).
Dependencies and Acceptance Criteria are stated per task. All event names, node types, and
contracts are canonical.

Phase tags map to the roadmap: MVP, V1, V2, V3, Future.

---

## Epic 1 — Canvas Editor

Compose architectures visually with typed nodes and edges (React Flow).

**Features:** node palette & placement; edge connection & validation; node/edge configuration.

| Task | Phase | Priority | Dependencies | Acceptance Criteria |
|------|-------|----------|--------------|---------------------|
| Render React Flow canvas shell with pan/zoom | MVP | P0 | Front-end scaffolding (`web/src/features/canvas`) | Canvas mounts, supports pan/zoom, ≥ 50 fps with 500 nodes (NFR-1) |
| Node palette for canonical `NodeType` enum | MVP | P0 | Canvas shell | Palette lists all canonical types; drag places a `Node` with default config |
| Directed `Edge` creation between nodes | MVP | P0 | Node placement | User connects nodes; `Edge` stores `sourceNodeId`/`targetNodeId` |
| Connection validation by node-type rules | MVP | P0 | Edge creation | Invalid links (e.g. `Consumer→Producer`) are rejected with an inline message |
| Node/Edge inspector configuration panels | MVP | P0 | Inspector feature (`features/inspector`) | Selecting a node opens type-specific config; changes persist to store |
| Canvas keyboard operability & ARIA | MVP | P1 | Canvas shell | All node/edge actions reachable by keyboard; focus visible; WCAG 2.1 AA (NFR-4) |
| Undo/redo of canvas edits | V1 | P2 | Zustand canvas store | Ctrl+Z/Ctrl+Y revert/redo last N edits deterministically |

---

## Epic 2 — Simulation Engine

Deterministic, event-driven runtime that emits canonical `SimulationEvent`s.

**Features:** logical clock & lifecycle; messaging semantics; event emission pipeline.

| Task | Phase | Priority | Dependencies | Acceptance Criteria |
|------|-------|----------|--------------|---------------------|
| `BackgroundService` engine loop with `Tick` clock | MVP | P0 | Domain/Application layers | Engine advances ticks and emits `TickAdvanced`; runs off the request thread |
| Lifecycle commands & events | MVP | P0 | Engine loop; MediatR | `start/pause/resume/stop` emit `SimulationStarted/Paused/Resumed/Stopped/Completed` |
| Canonical event envelope + monotonic `sequence` | MVP | P0 | Engine loop | Every event carries the full envelope; `sequence` is monotonic per simulation |
| RabbitMQ messaging semantics | MVP | P0 | Envelope; RabbitMQ adapter | Emits `MessagePublished`→`MessageRouted`→`MessageEnqueued`→`MessageDequeued`→`MessageReceived`→`MessageProcessed`→`AckReceived` per AMQP rules |
| REST/HTTP interaction semantics | MVP | P0 | Envelope | Emits `HttpRequestStarted`/`HttpResponseReceived`/`HttpRequestFailed`/`HttpRequestTimedOut` |
| Deterministic replay of a ticked run in tests | MVP | P1 | Engine loop | Same scenario + seed produces identical event sequence (xUnit, NFR-8) |
| Node state machine (`NodeActivated`/`NodeStateChanged`/`NodeFailed`/`NodeRecovered`) | V1 | P1 | Engine loop | Node lifecycle events emitted and reflected in state |

---

## Epic 3 — Messaging: RabbitMQ & Kafka

Real broker semantics via infrastructure adapters.

**Features:** RabbitMQ (AMQP) adapter; Kafka adapter; broker configuration.

| Task | Phase | Priority | Dependencies | Acceptance Criteria |
|------|-------|----------|--------------|---------------------|
| RabbitMQ adapter (exchanges, queues, routing keys, DLX) | MVP | P0 | `rabbitmq` container; Application ports | Routing by `Routing Key`/binding matches real AMQP; verified via Testcontainers |
| `Exchange`/`Queue`/`DeadLetterQueue` node behaviors | MVP | P0 | RabbitMQ adapter | Nodes enqueue/route/dead-letter consistent with config |
| Kafka adapter (topics, partitions, offsets, consumer groups) | V1 | P0 | `kafka` (+`zookeeper`/KRaft) container | `Topic`/`Partition` nodes; `ConsumerRegistered` reflects group/offset semantics |
| Partition assignment & offset progression visualization | V1 | P1 | Kafka adapter | Messages distribute across partitions; offsets advance per consumer group |
| RabbitMQ vs Kafka side-by-side scenario | V1 | P2 | Both adapters | Catalog scenario shows routing vs partitioning behavior differences |

---

## Epic 4 — Realtime Streaming

Push authoritative events to the client with ordering and recovery.

**Features:** SignalR hub & groups; batched delivery; reconnection/backfill.

| Task | Phase | Priority | Dependencies | Acceptance Criteria |
|------|-------|----------|--------------|---------------------|
| `SimulationHub` at `/hubs/simulation` with groups | MVP | P0 | Engine event pipeline | `Subscribe`/`Unsubscribe` join/leave per-`simulationId` group |
| `ReceiveSimulationEvent` server→client push | MVP | P0 | Hub | Client receives events in `sequence` order; P95 emit→render ≤ 250 ms (NFR-1) |
| `SimulationStateChanged` push | MVP | P0 | Hub | State transitions delivered as `SimulationStateDto` |
| Batched `ReceiveSimulationEvents` for high throughput | V1 | P1 | Hub | Engine coalesces bursts; sustains ≥ 2,000 events/s per simulation |
| Client reconnect + backfill via `?fromSequence=` | V1 | P0 | REST events endpoint | After reconnect, client detects gaps and backfills; no observable loss (NFR-5) |
| Redis SignalR backplane for horizontal scale | V2 | P1 | `redis` container | Any API instance serves any group; ≥ 200 concurrent simulations (NFR-2) |

---

## Epic 5 — Patterns: Retry, DLQ, CQRS, Saga, Circuit Breaker

Teach resilience and orchestration through observable behavior.

**Features:** retry/DLQ; Circuit Breaker; Saga; CQRS; Event Sourcing.

| Task | Phase | Priority | Dependencies | Acceptance Criteria |
|------|-------|----------|--------------|---------------------|
| Retry with backoff + DLQ flow | V1 | P0 | RabbitMQ adapter; `DeadLetterQueue` node | Emits `MessageNacked`→`RetryScheduled`→`MessageRetried`→`DeadLettered`; token moves to DLQ |
| `MessageExpired`/`MessageDropped` handling | V1 | P1 | Queue behaviors | TTL/overflow produce the correct events |
| Circuit Breaker state machine | V2 | P0 | HTTP/RPC semantics | Emits `CircuitBreakerOpened`/`HalfOpened`/`Closed` per failure thresholds |
| Saga orchestration with compensation | V2 | P0 | Multi-service scenario; MediatR | Emits `SagaStarted`/`SagaStepCompleted`/`SagaCompensationTriggered`/`SagaCompleted` |
| CQRS command/query split visualization | V2 | P0 | MediatR Application layer | Separate read/write paths rendered; commands and queries distinguishable |
| gRPC interaction semantics | V2 | P1 | HTTP/RPC semantics | Emits `GrpcCallStarted`/`GrpcCallCompleted` |
| Event Sourcing store + projections | V3 | P1 | Persisted event history | Append-only store and projection nodes visualized |

---

## Epic 6 — UI / UX & Animation

Make behavior legible, smooth, and accessible.

**Features:** message-token animation; event inspector/timeline; metrics dashboard; fault UI.

| Task | Phase | Priority | Dependencies | Acceptance Criteria |
|------|-------|----------|--------------|---------------------|
| Framer Motion message-token animation on edges | MVP | P0 | Realtime events | Tokens animate strictly from received `SimulationEvent`s; each maps to an `eventId` |
| Presentation-only `AnimationStarted`/`AnimationFinished` | MVP | P1 | Token animation | Derived on client; never introduce domain state |
| Event inspector showing live `Timeline` | MVP | P0 | Realtime events | Lists events in order; selecting one shows full envelope |
| `prefers-reduced-motion` support | MVP | P1 | Token animation | Motion reduced/removed while event legibility preserved (NFR-4) |
| Metrics dashboard from `MetricSnapshot` | V1 | P0 | Metrics endpoint | Renders throughput/avgLatencyMs/inFlight/dlqCount/retries live |
| Fault-injection UI (target node/edge, choose fault) | V2 | P0 | Faults endpoint | Produces `FaultInjected`/`LatencyInjected`/`PartitionCreated`/`PartitionHealed`/`NodeFailed`/`NodeRecovered` |
| Timeline scrubbing & deterministic replay | V2 | P0 | Persisted events | Scrub to `sequence` reconstructs state identically on repeat |

---

## Epic 7 — Dev / Ops & Platform

Production-grade delivery, scale, and observability.

**Features:** local dev stack; persistence; CI; observability; auth & tenancy.

| Task | Phase | Priority | Dependencies | Acceptance Criteria |
|------|-------|----------|--------------|---------------------|
| Docker Compose stack (`web`,`api`,`rabbitmq`,`postgres`) | MVP | P0 | — | `docker compose up` brings up a runnable MVP stack |
| Add `kafka`(+`zookeeper`/KRaft) & `redis` to stack | V1 | P0 | MVP stack | Full canonical container set runs locally |
| EF Core (PostgreSQL) persistence for scenarios/simulations | V1 | P0 | `postgres` container | `Scenario`/`Simulation`/`SimulationEvent` persisted; migrations applied |
| GitHub Actions CI (build, test, lint) | MVP | P0 | Repo | PRs run unit + integration (Testcontainers) + Playwright; green required to merge |
| OpenTelemetry traces/metrics/logs + Serilog | V1 | P1 | API/engine | Spans correlate via `traceId`/`correlationId`; structured logs emitted (NFR-6) |
| AuthN/AuthZ + per-user ownership | V3 | P0 | API | OIDC login; users only access their `Scenario`s/`Simulation`s (NFR-3) |
| Rate limiting on mutating & fault endpoints | V3 | P1 | API | Excess requests throttled with RFC 7807 responses |
| Cloud multi-tenant SaaS (tenancy, SSO, seats) | Future | P2 | AuthZ; billing | Organization isolation and seat management GA |

---

## Epic 8 — Learning Content & Catalog

Turn the engine into structured education.

**Features:** concept catalog; guided lessons; assessments; classrooms.

| Task | Phase | Priority | Dependencies | Acceptance Criteria |
|------|-------|----------|--------------|---------------------|
| `Catalog` of concept `Scenario` templates | V1 | P0 | Scenario save/load | `GET /api/v1/catalog` lists concept-tagged scenarios; instantiable as editable `Scenario`s |
| Scenario save/load/tag/delete | V1 | P0 | EF Core persistence | Full CRUD via `/api/v1/scenarios`; `conceptTag` searchable |
| Guided lessons attached to catalog entries | V3 | P0 | Catalog | Lessons sequence objectives and load scenario states step by step |
| Exercises with expected observations | V3 | P1 | Guided lessons | Each step states a learning objective and expected `SimulationEvent` observation |
| Assessments verifying learner predictions | V3 | P0 | Exercises | Prediction checks recorded; comprehension lift measurable (KPI) |
| Multi-user classrooms & assignment | V3 | P0 | AuthZ; catalog | Instructors assign scenarios and review learner `Timeline`s |
| Collaboration, plugin SDK, marketplace | Future | P2 | Classrooms; stable extension contract | Co-editing, custom `NodeType` plugins, and scenario marketplace available |

## Related documents

- [Vision](./vision.md)
- [Product Requirements Document](./prd.md)
- [Personas](./personas.md)
- [Roadmap](./roadmap.md)
- [Glossary](./glossary.md)
- [Architecture](../02-architecture/architecture.md)
- [Event Model](../02-architecture/event-model.md)
