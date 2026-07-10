# Distributed Flow Lab — Product Requirements Document (PRD)

> Status: Living document. This PRD governs the MVP and Version 1 scope in detail and frames
> later versions at a summary level. It is bound by the shared documentation canon: all event
> names, node types, contracts, and terminology are canonical and must not be altered here.

## Overview

Distributed Flow Lab (DFL) is an educational SaaS platform for learning distributed systems
through interactive visual simulations. Users compose architectures on a **canvas** using typed
**Node**s and **Edge**s, then execute a **Simulation** whose visual behavior is driven
**exclusively** by backend **SimulationEvent**s streamed over SignalR. The product pairs a React
Flow front end with an ASP.NET 8 event-driven engine backed by real infrastructure adapters
(RabbitMQ, Kafka, Redis).

This PRD defines the functional and non-functional requirements, user stories, acceptance
criteria, risks, and constraints for delivering DFL to production quality. The delivery
sequence is governed by [Roadmap](./roadmap.md) and decomposed into work in
[Backlog](./backlog.md).

## Business Objectives

- **BO-1 — Establish DFL as a credible learning platform.** Deliver an MVP that visibly and
  correctly teaches core messaging (RabbitMQ + REST) with event-driven animation, proving the
  "backend is the single source of truth" thesis.
- **BO-2 — Drive measurable learning outcomes.** Achieve the comprehension-lift and completion
  KPIs defined in [Vision — Success Metrics](./vision.md#success-metrics).
- **BO-3 — Grow engaged learners.** Reach the retention and session-length KPIs through a
  compelling `Catalog` and safe `Fault Injection`.
- **BO-4 — Enable monetizable classroom features.** Ship instructor and classroom capabilities
  (V3) that support paid seats for bootcamps and enterprises.
- **BO-5 — Build an extensible foundation.** Keep the architecture clean enough that new
  `NodeType`s, patterns, and adapters can be added without breaking existing scenarios,
  culminating in a plugin SDK (Future).

## Functional Requirements

Requirements are numbered and tagged with the roadmap phase that first delivers them.

### Canvas & authoring
- **FR-1 (MVP)** — Users can create a `Scenario` on a React Flow canvas by placing typed `Node`s
  from a palette. Supported types are the canonical `NodeType` enum: `Producer`, `Consumer`,
  `Service`, `ApiGateway`, `LoadBalancer`, `Exchange`, `Queue`, `Topic`, `Partition`, `Broker`,
  `Database`, `Cache`, `DeadLetterQueue`, `Client`.
- **FR-2 (MVP)** — Users can connect `Node`s with directed `Edge`s (e.g. `Producer→Exchange`,
  `Service→Database`) and edit `Edge` labels/config.
- **FR-3 (MVP)** — The canvas validates connections against type rules (e.g. a `Producer` may
  connect to an `Exchange`; a `Queue` binds from an `Exchange`) and surfaces invalid links.
- **FR-4 (MVP)** — Each `Node` exposes a type-specific configuration panel in the inspector
  (e.g. `Queue` prefetch, `Consumer` processing time, `Exchange` type/routing).
- **FR-5 (V1)** — Users can save, name, tag (`conceptTag`), load, update, and delete `Scenario`s
  via the REST API (`/api/v1/scenarios`).

### Catalog & learning content
- **FR-6 (V1)** — Users can browse a `Catalog` of concept-focused `Scenario` templates via
  `GET /api/v1/catalog` and instantiate one as an editable `Scenario`.
- **FR-7 (V3)** — Guided lessons, exercises, and assessments are attached to `Catalog` entries.

### Simulation lifecycle
- **FR-8 (MVP)** — Users can create a `Simulation` from a `Scenario`
  (`POST /api/v1/simulations`) and control it via `start`, `pause`, `resume`, `stop` endpoints.
- **FR-9 (MVP)** — The simulation engine runs as a `BackgroundService` advancing a logical
  clock, emitting `TickAdvanced` and lifecycle events (`SimulationStarted`, `SimulationPaused`,
  `SimulationResumed`, `SimulationStopped`, `SimulationCompleted`).
- **FR-10 (MVP)** — Every state change in the running system emits a canonical `SimulationEvent`
  using the canonical envelope (with `eventId`, `simulationId`, `sequence`, `tick`,
  `occurredAt`, `type`, `sourceNodeId`, `targetNodeId`, `correlationId`, `traceId`, `payload`).

### Messaging behavior (event-driven core)
- **FR-11 (MVP)** — RabbitMQ-style messaging emits `MessagePublished`, `MessageRouted`,
  `MessageEnqueued`, `MessageDequeued`, `MessageReceived`, `MessageProcessed`, and `AckReceived`
  reflecting real AMQP semantics (exchange → routing key → queue binding).
- **FR-12 (MVP)** — REST/HTTP interactions emit `HttpRequestStarted`, `HttpResponseReceived`,
  `HttpRequestFailed`, and `HttpRequestTimedOut`.
- **FR-13 (V1)** — Kafka-style messaging emits messaging events across `Topic`/`Partition`
  nodes with `ConsumerRegistered` semantics reflecting offsets and consumer groups.
- **FR-14 (V1)** — Redis `Cache` nodes emit `CacheHit`, `CacheMiss`, and `CacheEvicted`.
- **FR-15 (V1)** — Retry and dead-lettering emit `MessageNacked`, `RetryScheduled`,
  `MessageRetried`, `DeadLettered`, `MessageExpired`, and `MessageDropped`, with a
  `DeadLetterQueue` node receiving dead-lettered messages.

### Resilience & advanced patterns
- **FR-16 (V2)** — Circuit Breaker behavior emits `CircuitBreakerOpened`,
  `CircuitBreakerHalfOpened`, and `CircuitBreakerClosed`.
- **FR-17 (V2)** — Saga orchestration emits `SagaStarted`, `SagaStepCompleted`,
  `SagaCompensationTriggered`, and `SagaCompleted`.
- **FR-18 (V2)** — CQRS scenarios model separate command/query paths (backed by MediatR in the
  Application layer) and visualize the read/write split.
- **FR-19 (V2)** — gRPC interactions emit `GrpcCallStarted` and `GrpcCallCompleted`.
- **FR-20 (V3)** — Event Sourcing scenarios visualize an append-only event store and projections.
- **FR-21 (V3)** — API Gateway scenarios model routing/fan-out across downstream `Service`s.

### Realtime streaming & animation
- **FR-22 (MVP)** — The client subscribes to a `Simulation` via the `SimulationHub` at
  `/hubs/simulation` (`Subscribe`/`Unsubscribe`), joining a SignalR group per `simulationId`.
- **FR-23 (MVP)** — The server pushes events via `ReceiveSimulationEvent` and, for high
  throughput, `ReceiveSimulationEvents` (batched); state changes via `SimulationStateChanged`.
- **FR-24 (MVP)** — The client animates `Message` tokens along `Edge`s and updates `Node` state
  strictly from received `SimulationEvent`s. It may derive presentation-only
  `AnimationStarted`/`AnimationFinished`, which never introduce new domain state.
- **FR-25 (MVP)** — An event inspector shows the live `Timeline` and lets users select any
  `SimulationEvent` to see its full envelope.

### Fault injection
- **FR-26 (V2)** — Users can inject faults via `POST /api/v1/simulations/{id}/faults`, producing
  `FaultInjected`, `LatencyInjected`, `PartitionCreated`, `PartitionHealed`, `NodeFailed`, and
  `NodeRecovered` as appropriate.
- **FR-27 (V2)** — A fault-injection UI lets users target a `Node`/`Edge` and choose a fault type
  and parameters without leaving the canvas.

### Metrics & timeline
- **FR-28 (V1)** — A metrics dashboard derives and displays `MetricSnapshot` values (throughput,
  avgLatencyMs, inFlight, dlqCount, retries) via `GET /api/v1/simulations/{id}/metrics`.
- **FR-29 (V2)** — Users can scrub and replay the `Timeline` using persisted events
  (`GET /api/v1/simulations/{id}/events?fromSequence=`), reconstructing state deterministically.

### Collaboration & platform (Future)
- **FR-30 (V3)** — Multi-user classrooms: instructors assign `Scenario`s and review learner
  timelines.
- **FR-31 (Future)** — Real-time collaborative editing, plugin SDK for custom nodes, and a
  community marketplace.

## Non-Functional Requirements

- **NFR-1 — Performance.** Event-to-animation P95 latency (server emit → client render)
  ≤ 250 ms. Canvas maintains ≥ 50 fps while interacting with a 500-node `Scenario`. The
  engine sustains ≥ 2,000 `SimulationEvent`s/second per simulation using batched hub delivery.
- **NFR-2 — Scalability.** The API and hub scale horizontally behind a load balancer; SignalR
  uses a backplane (Redis) so any node can serve any `simulationId` group. A single API
  instance supports ≥ 200 concurrent active simulations.
- **NFR-3 — Security.** All traffic over TLS. Authenticated access (OAuth2/OIDC) with per-user
  ownership of `Scenario`s and `Simulation`s. Input validated with FluentValidation; errors
  returned as RFC 7807 problem+json. No secrets in the client; adapter credentials server-side
  only. Rate limiting on mutating endpoints and fault injection.
- **NFR-4 — Accessibility.** WCAG 2.1 AA: keyboard operability of the canvas and controls,
  visible focus, ARIA roles on interactive nodes, color-contrast-compliant design tokens, and
  motion-reduction support (`prefers-reduced-motion`) that degrades Framer Motion animations
  gracefully while preserving event legibility.
- **NFR-5 — Reliability.** Event delivery preserves per-simulation `sequence` ordering with no
  observable gaps ≥ 99.9% of the time. SignalR clients auto-reconnect and backfill missed events
  via `GET /events?fromSequence=`. Target crash-free session rate ≥ 99.5%.
- **NFR-6 — Observability.** OpenTelemetry traces/metrics/logs across API, engine, and adapters;
  Serilog structured logging. Every `SimulationEvent` carries `traceId`/`correlationId` for
  end-to-end correlation. Health checks and dashboards for engine throughput and hub delivery.
- **NFR-7 — Maintainability & Architecture.** Clean Architecture enforced by the dependency rule
  (`Api → Infrastructure → Application → Domain`; Domain depends on nothing). Business logic
  never in controllers/hubs. SOLID, DI, and MediatR-based CQRS in the Application layer.
- **NFR-8 — Testability.** Domain and Application covered by xUnit + FluentAssertions; adapters
  and API by Testcontainers-backed integration tests; front end by Vitest + React Testing
  Library and Playwright E2E. Deterministic engine ticks make simulations reproducible in tests.
- **NFR-9 — Portability & Local Dev.** The full stack (`web`, `api`, `rabbitmq`, `kafka`
  (+`zookeeper`/KRaft), `redis`, `postgres`) runs via Docker Compose with a single command.
- **NFR-10 — Internationalization readiness.** UI strings externalized to enable future
  localization; canonical event `type` names remain invariant (they are contract, not copy).

## User Stories

Personas are the canonical set (see [Personas](./personas.md)).

- **US-1 (Beginner Developer):** *As a Beginner Developer, I want to drag a `Producer`, an
  `Exchange`, a `Queue`, and a `Consumer` onto the canvas and press Run, so that I can see a
  message actually travel and be acknowledged.*
- **US-2 (Beginner Developer):** *As a Beginner Developer, I want plain-language tooltips on each
  `NodeType`, so that I understand what each component does before I connect them.*
- **US-3 (Backend Engineer):** *As a Backend Engineer, I want to configure `Consumer` processing
  time and queue prefetch, so that I can observe back-pressure and how a `Queue` builds depth.*
- **US-4 (Backend Engineer):** *As a Backend Engineer, I want to enable retry with a `DeadLetterQueue`,
  so that I can watch `RetryScheduled` → `MessageRetried` → `DeadLettered` and understand
  poison-message handling.*
- **US-5 (Software Architect):** *As a Software Architect, I want to compose a Saga across
  multiple `Service`s and inject a failure, so that I can demonstrate compensation
  (`SagaCompensationTriggered`) to my team.*
- **US-6 (Software Architect):** *As a Software Architect, I want to compare a RabbitMQ routing
  topology against a Kafka partitioned topology side by side, so that I can justify a design
  trade-off with observed behavior.*
- **US-7 (Instructor):** *As an Instructor, I want to save a `Scenario` to the `Catalog` and
  assign it to a classroom, so that my students all start from the same topology.*
- **US-8 (Instructor):** *As an Instructor, I want to scrub the `Timeline` of a completed
  `Simulation`, so that I can pause on a `DeadLettered` event and explain exactly why it happened.*
- **US-9 (Engineering Student):** *As an Engineering Student, I want a guided lesson that walks me
  from a single queue to a Circuit Breaker, so that I build understanding incrementally.*
- **US-10 (Engineering Student):** *As an Engineering Student, I want an assessment that checks I
  can predict what happens when a `Consumer` fails, so that I know I actually learned it.*

## Acceptance Criteria

Given/When/Then criteria tied to key stories.

### AC for US-1 (run first messaging flow)
- **Given** a valid `Scenario` with `Producer→Exchange→Queue→Consumer`,
  **When** the user creates a `Simulation` and calls `start`,
  **Then** the client receives, in `sequence` order, at least `SimulationStarted`,
  `MessagePublished`, `MessageRouted`, `MessageEnqueued`, `MessageDequeued`, `MessageReceived`,
  `MessageProcessed`, and `AckReceived`, and a token is animated along each corresponding `Edge`.
- **Given** the simulation is running, **When** any animation renders, **Then** it corresponds to
  a received `SimulationEvent` (no client-invented state), verifiable by matching each
  `AnimationStarted` to a prior backend event `eventId`.

### AC for US-3 (back-pressure)
- **Given** a `Consumer` processing time greater than the `Producer` publish interval,
  **When** the simulation runs, **Then** the `Queue` node's depth metric increases over
  successive `TickAdvanced` events and `inFlight` in the `MetricSnapshot` grows accordingly.

### AC for US-4 (retry and DLQ)
- **Given** a `Consumer` configured to fail processing and a `Queue` bound to a `DeadLetterQueue`
  with a max-retry policy, **When** processing fails, **Then** the engine emits `MessageNacked`,
  then `RetryScheduled` and `MessageRetried` up to the retry limit, then `DeadLettered`, and the
  message token visibly moves to the `DeadLetterQueue` node.

### AC for US-5 (Saga compensation)
- **Given** a Saga scenario across three `Service`s, **When** a fault is injected on step 2 via
  `POST /api/v1/simulations/{id}/faults`, **Then** the engine emits `SagaStarted`,
  `SagaStepCompleted` (step 1), a failure event on step 2, `SagaCompensationTriggered`, and the
  timeline shows compensation flowing in reverse before `SimulationCompleted`.

### AC for US-8 (timeline scrubbing)
- **Given** a completed `Simulation` with persisted events, **When** the instructor scrubs to a
  `sequence` position, **Then** the canvas deterministically reconstructs node/message state as
  of that `sequence` using `GET /api/v1/simulations/{id}/events?fromSequence=`, with identical
  results on repeated scrubs.

### AC for US-9 (guided lesson)
- **Given** a guided lesson attached to a `Catalog` entry, **When** the student advances a step,
  **Then** the platform loads the corresponding `Scenario` state and presents the step's learning
  objective and expected observation before enabling Run.

## Risks

| ID | Risk | Impact | Likelihood | Mitigation |
|----|------|--------|-----------|------------|
| R-1 | Event volume overwhelms the client, degrading fps | High | Medium | Batched `ReceiveSimulationEvents`; client-side coalescing; virtualization; NFR-1 budgets enforced in Playwright perf tests |
| R-2 | SignalR reconnection loses events, breaking the "source of truth" guarantee | High | Medium | Monotonic `sequence` + backfill via `?fromSequence=`; gap detection on client; Redis backplane |
| R-3 | Real broker adapters make behavior nondeterministic and hard to test | Medium | High | Deterministic logical `Tick` clock; adapter abstraction behind Application ports; Testcontainers integration tests |
| R-4 | Scope creep across patterns dilutes MVP quality | High | High | Strict phase gating per [Roadmap](./roadmap.md); one feature at a time; exit criteria per phase |
| R-5 | Simulation fidelity gap erodes trust ("this isn't how real Kafka behaves") | High | Medium | Ground semantics in real adapters; document simplifications explicitly; SME review of each concept |
| R-6 | Accessibility retrofitted late becomes costly | Medium | Medium | NFR-4 enforced from MVP; design tokens with contrast; keyboard-first canvas review each phase |
| R-7 | Persistence of high-volume events grows unbounded | Medium | Medium | Event retention policy per simulation; snapshotting via `MetricSnapshot`; archival for completed simulations |
| R-8 | Multi-tenant security defects expose user scenarios | High | Low | Per-user ownership checks, authz tests, rate limiting (NFR-3); security review before V3 classrooms |

## Constraints

- **Technology is fixed by canon.** Frontend: React 18 + TypeScript + Vite, React Flow, Zustand,
  `@microsoft/signalr`, Tailwind, Framer Motion, Vitest/RTL/Playwright. Backend: ASP.NET 8
  Minimal APIs, SignalR, MediatR, EF Core (PostgreSQL), `BackgroundService`, FluentValidation,
  xUnit/FluentAssertions/Testcontainers. Adapters: RabbitMQ, Kafka, Redis. DevOps: Docker
  Compose, GitHub Actions, OpenTelemetry, Serilog. Deviation requires an ADR.
- **Architecture is fixed by canon.** Clean Architecture dependency rule and the defined
  solution/front-end structure must be respected; no business logic in controllers or hubs.
- **Contracts are fixed by canon.** The event envelope, Event Catalog names, `NodeType` enum,
  SignalR hub contract, and REST shapes are authoritative and may not be renamed here.
- **Frontend must not invent state.** All domain state originates from backend `SimulationEvent`s.
- **Documentation-first.** Features are documented and gated by backlog priority before build.

## Future Roadmap

The delivery sequence uses the canonical phases **MVP → Version 1 → Version 2 → Version 3 →
Future**. In summary: the **MVP** proves the event-driven core with RabbitMQ + REST messaging,
the canvas editor, SignalR streaming, basic animations, and the event inspector. **Version 1**
adds Kafka, Redis cache, retry/DLQ, the metrics dashboard, scenario save/load, and the catalog.
**Version 2** adds CQRS, Saga, Circuit Breaker, gRPC, the fault-injection UI, and timeline
scrubbing/replay. **Version 3** adds Event Sourcing, API Gateway, multi-user classrooms, guided
lessons/exercises, and assessments. **Future** covers collaboration, a plugin SDK for custom
nodes, a marketplace, and cloud multi-tenant SaaS. Full objectives, dependencies, and exit
criteria are defined in [Roadmap](./roadmap.md); work items are tracked in
[Backlog](./backlog.md).

## Related documents

- [Vision](./vision.md)
- [Personas](./personas.md)
- [Roadmap](./roadmap.md)
- [Backlog](./backlog.md)
- [Glossary](./glossary.md)
- [Architecture](../02-architecture/architecture.md)
- [Event Model](../02-architecture/event-model.md)
