# Technology Stack

This document is the engineering reference for the **canonical technology stack** of
**Distributed Flow Lab (DFL)** (canon §2). For each technology it states **what it is**, **why
it was chosen** (justification tied to DFL's goals), and links the **Architecture Decision
Record** that ratifies the choice.

The stack is **canonical — do not deviate.** New dependencies require an ADR.

---

## 1. Stack at a glance

| Area | Technology | Role | ADR |
|------|-----------|------|-----|
| Frontend | React 18 + TypeScript | UI framework + type safety | [ADR-010](../adr/ADR-010-frontend-stack.md) |
| Frontend | Vite | Build tool / dev server | [ADR-010](../adr/ADR-010-frontend-stack.md) |
| Frontend | React Flow | Node/edge canvas | [ADR-010](../adr/ADR-010-frontend-stack.md) |
| Frontend | Zustand | Client state management | [ADR-010](../adr/ADR-010-frontend-stack.md) |
| Frontend | @microsoft/signalr | Realtime client transport | [ADR-002](../adr/ADR-002-signalr.md) |
| Frontend | Tailwind CSS | Styling + design tokens | [ADR-010](../adr/ADR-010-frontend-stack.md) |
| Frontend | Framer Motion | Message/edge animation | [ADR-010](../adr/ADR-010-frontend-stack.md) |
| Backend | ASP.NET 8 (Minimal APIs) | HTTP host + REST API | [ADR-004](../adr/ADR-004-clean-architecture.md) |
| Backend | SignalR | Realtime push (server) | [ADR-002](../adr/ADR-002-signalr.md) |
| Backend | MediatR | CQRS in Application layer | [ADR-008](../adr/ADR-008-cqrs-mediatr.md) |
| Backend | EF Core (PostgreSQL) | Persistence | [ADR-011](../adr/ADR-011-postgres-efcore.md) |
| Backend | IHostedService / BackgroundService | Simulation runtime loop | [ADR-007](../adr/ADR-007-background-service-engine.md) |
| Backend | FluentValidation | Input validation | [ADR-008](../adr/ADR-008-cqrs-mediatr.md) |
| Messaging | RabbitMQ | AMQP broker adapter | [ADR-003](../adr/ADR-003-rabbitmq.md) |
| Messaging | Apache Kafka | Log/stream broker adapter | [ADR-003](../adr/ADR-003-rabbitmq.md) |
| Messaging | Redis | Cache + pub/sub adapter | [ADR-003](../adr/ADR-003-rabbitmq.md) |
| Persistence | PostgreSQL | Relational store | [ADR-011](../adr/ADR-011-postgres-efcore.md) |
| DevOps | Docker + Docker Compose | Local dev + orchestration | [ADR-005](../adr/ADR-005-docker-compose.md) |
| DevOps | GitHub Actions | CI/CD | [ADR-005](../adr/ADR-005-docker-compose.md) |
| Observability | OpenTelemetry | Traces / metrics / logs | [ADR-012](../adr/ADR-012-observability-opentelemetry.md) |
| Observability | Serilog | Structured logging | [ADR-012](../adr/ADR-012-observability-opentelemetry.md) |
| Testing | xUnit + FluentAssertions + Testcontainers | Backend tests | [ADR-013](../adr/ADR-013-testing-strategy.md) |
| Testing | Vitest + React Testing Library + Playwright | Frontend tests | [ADR-013](../adr/ADR-013-testing-strategy.md) |

---

## 2. Frontend

**React 18 + TypeScript** is the UI foundation. React's declarative, component model fits a
canvas that continuously re-renders from a stream of `SimulationEvent`s, and TypeScript makes the
client `domain/` types a compile-time mirror of the backend event envelope — preventing drift
between what the engine emits and what the UI renders. React 18 concurrent features help keep the
canvas responsive under high event throughput.

**Vite** is the build tool and dev server. It gives near-instant HMR for fast iteration and
produces optimized, code-split production bundles, supporting the platform's performance goals
(lazy loading, minimal bundle cost).

**React Flow** is the node/edge canvas library. DFL's core interaction *is* a graph of `Node`s
(`Producer`, `Consumer`, `Exchange`, `Queue`, `Topic`, `Broker`, …) connected by `Edge`s. React
Flow provides pan/zoom, custom node/edge renderers, and handles, so we build DFL's semantics on a
proven canvas instead of reinventing graph interaction.

**Zustand** manages client state. Its minimal, hook-based API and fine-grained selectors let the
`canvasStore`, `simulationStore`, and `uiStore` update precisely from incoming events without the
boilerplate or broad re-renders of heavier state libraries — directly supporting the "avoid
unnecessary renders" performance rule.

**@microsoft/signalr** is the realtime client. It is the official client for the ASP.NET SignalR
hub (`/hubs/simulation`), giving automatic transport negotiation (WebSockets first) and
reconnection so the frontend reliably receives `ReceiveSimulationEvent` /
`ReceiveSimulationEvents`.

**Tailwind CSS** provides styling and design tokens. Utility-first styling with a shared token
config yields a consistent design system and fast, composable UI without bespoke CSS sprawl.

**Framer Motion** animates message tokens and edges. It produces smooth, declarative,
interruptible animations — but each animation is **triggered by a received backend event**; the
only client-side presentation events are `AnimationStarted` / `AnimationFinished`, which describe
rendering, never new state.

---

## 3. Backend

**ASP.NET 8 with Minimal APIs** hosts the platform and exposes the REST surface under
`/api/v1` (canon §9). Minimal APIs keep endpoints thin and declarative, reinforcing that
**business logic never lives in controllers** — endpoints delegate to MediatR. .NET 8's
performance and first-class DI/hosting model underpin the whole backend.

**SignalR** pushes events to the browser in realtime. It is the transport for the source-of-truth
pipeline: the engine emits a `SimulationEvent`, the `SimulationHub` broadcasts it to the
per-`simulationId` group, and the client renders it. SignalR's group model maps cleanly onto
"one group per simulation."

**MediatR** implements **CQRS** inside the Application layer: each use case (create scenario,
start/pause/resume/stop a simulation, inject a fault, query events/metrics) is a command or query
with a dedicated handler. This enforces single-responsibility handlers and keeps orchestration out
of the host, and enables pipeline behaviors (validation, logging).

**EF Core (PostgreSQL)** persists `Scenario`/`Simulation` metadata and the append-only
`SimulationEvent` stream (with `sequence` per simulation). EF Core gives a productive,
strongly-typed data layer with migrations; PostgreSQL provides the relational integrity and
JSON support the model needs.

**IHostedService / BackgroundService** runs the **simulation runtime loop**. The engine advances
the logical `Tick` clock, executes node behavior, and emits events off the request thread — the
right primitive for a long-running, event-producing process that must be deterministic and
cancellable.

**FluentValidation** validates inbound requests/commands with expressive, testable rules,
surfaced as RFC 7807 problem responses at the API boundary.

---

## 4. Real infrastructure adapters (messaging)

These adapters make simulations reflect **genuine broker semantics** rather than a caricature
(canon §2). They implement Application ports and live in `Infrastructure/Messaging`.

**RabbitMQ** models AMQP: **exchanges, queues, routing keys, and dead-letter exchanges (DLX)**.
It grounds events such as `MessagePublished`, `MessageRouted`, `MessageEnqueued`, `AckReceived`,
`MessageNacked`, `RetryScheduled`, and `DeadLettered` in real routing/ack behavior.

**Apache Kafka** models the log/stream world: **topics, partitions, offsets, and consumer
groups**. It lets DFL teach ordering, partitioning, and consumer-group rebalancing with authentic
semantics — the contrast with RabbitMQ is itself a learning objective.

**Redis** provides **cache and pub/sub**, grounding `CacheHit`, `CacheMiss`, `CacheEvicted`, and
lightweight fan-out patterns.

Using real brokers (behind ports) means learners observe true behavior while the engine remains
decoupled and testable — see [ADR-003](../adr/ADR-003-rabbitmq.md).

---

## 5. Persistence

**PostgreSQL** is the relational store for `Scenario`, `Simulation`, persisted `SimulationEvent`s,
and `MetricSnapshot`s. It is open-source, container-friendly, transactionally strong, and its
JSON/JSONB support suits storing type-specific node/edge `config` and event `payload`s while
keeping the core columns (`sequence`, `simulationId`, `tick`, `type`) queryable for replay
(`GET /api/v1/simulations/{id}/events?fromSequence=`). See
[ADR-011](../adr/ADR-011-postgres-efcore.md).

---

## 6. Platform / DevOps

**Docker + Docker Compose** provide local development and container orchestration. A single
`docker compose up` brings up the full dependency set — `web`, `api`, `rabbitmq`, `kafka`
(KRaft or with `zookeeper`), `redis`, `postgres` — giving every engineer and CI an identical,
reproducible environment. See [ADR-005](../adr/ADR-005-docker-compose.md) and [Docker](./docker.md).

**GitHub Actions** runs CI/CD: build, `dotnet format`/analyzers, `tsc`/ESLint, the test gates,
container image build/push, and deployment. It lives beside the code in `.github/workflows/` and
is detailed in [Deployment](./deployment.md).

---

## 7. Observability

**OpenTelemetry** instruments the backend for **traces, metrics, and logs**, propagating the
event envelope's `traceId`/`correlationId` so a `Message`'s journey through nodes is traceable
end-to-end and product/performance KPIs (e.g. event-to-animation latency) are measurable.

**Serilog** provides **structured logging** with enriched properties (`simulationId`,
`sequence`, `traceId`), integrated with the OpenTelemetry pipeline. Together they make the
platform observable in the same event-driven spirit it teaches. See
[ADR-012](../adr/ADR-012-observability-opentelemetry.md).

---

## 8. Testing tools

**Backend:** **xUnit** (test framework), **FluentAssertions** (expressive, readable assertions),
and **Testcontainers** (spin up real RabbitMQ/Kafka/Redis/PostgreSQL in integration tests so
adapters are verified against real brokers).

**Frontend:** **Vitest** (fast, Vite-native unit/component runner), **React Testing Library**
(behavior-focused component testing), and **Playwright** (cross-browser E2E of the full
canvas → simulation → animation flow).

The complete strategy, coverage targets, and CI gates are in [Testing](./testing.md) and
[ADR-013](../adr/ADR-013-testing-strategy.md).

---

## Related documents

- [Coding Standards](./coding-standards.md)
- [Folder Structure](./folder-structure.md)
- [Local Development](./local-development.md)
- [Docker](./docker.md)
- [Testing](./testing.md)
- [Deployment](./deployment.md)
- [Architecture](../02-architecture/architecture.md)
- [Architecture Decision Records](../adr/)
