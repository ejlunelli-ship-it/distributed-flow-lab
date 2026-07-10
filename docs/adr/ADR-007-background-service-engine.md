# ADR-007: Simulation runtime as an ASP.NET BackgroundService tick loop

- **Status:** Accepted
- **Date:** 2026-07-07

## Context

The Simulation Engine must produce an authoritative, ordered, replayable stream of
`SimulationEvent`s and is the single source of truth for all simulation state
([ADR-006](./ADR-006-backend-source-of-truth.md), canon §1). A `Simulation` has a real
lifecycle — `Draft|Running|Paused|Completed|Stopped|Failed` (canon §10) — and advances on a
discrete logical clock, the `Tick` (canon §5). Each tick, the engine evaluates node
behaviors, moves `Message`s, and emits canonical events (`TickAdvanced`,
`MessagePublished`, `MessageEnqueued`, `MessageDequeued`, `AckReceived`, `RetryScheduled`,
`DeadLettered`, …; canon §7) into the `Timeline`, from which they are pushed to clients over
`SimulationHub` (canon §8) and persisted (canon §10).

This work is fundamentally **long-running, stateful, and asynchronous to any single HTTP
request**. A learner issues `POST /api/v1/simulations/{id}/start` (canon §9) and then
watches events arrive over seconds or minutes; the request that started the run must not own
the run. The runtime must:

- Advance ticks on a controlled cadence, decoupled from client request threads.
- Respond to lifecycle commands (`pause`, `resume`, `stop`; canon §9) between ticks.
- Emit events **in a deterministic order** so `sequence`/`tick` (canon §6) are meaningful
  and replay is exact ([ADR-009](./ADR-009-event-envelope-sequencing.md)).
- Live within the Clean Architecture layering: the runtime implementation belongs in
  `DistributedFlowLab.Infrastructure`, behind Application ports (canon §3,
  [ADR-004](./ADR-004-clean-architecture.md)).

ASP.NET 8 offers a first-class primitive for exactly this shape of work:
`IHostedService` / `BackgroundService` (canon §2; CLAUDE.md Backend Principles — Background
Services). This ADR records adopting it for the simulation runtime.

## Decision

We implement the simulation runtime as a hosted **`BackgroundService`** (an
`IHostedService`) in `DistributedFlowLab.Infrastructure` that runs a **tick loop**
advancing the logical `Tick` clock and emitting `SimulationEvent`s.

- **Tick loop.** A hosted background worker owns the cadence. On each iteration it advances
  the clock (emitting `TickAdvanced`), asks each active `Node`'s behavior to react to
  pending `Message`s, and appends the resulting canonical events to the `Timeline`. The tick
  is the engine's unit of determinism (canon §5): all events produced within a tick are
  assigned a monotonic `sequence` and stamped with that `tick`
  ([ADR-009](./ADR-009-event-envelope-sequencing.md)).
- **Lifecycle as state, checked between ticks.** Commands from the REST endpoints
  (`start`, `pause`, `resume`, `stop`; canon §9) are delivered as MediatR commands
  ([ADR-008](./ADR-008-cqrs-mediatr.md)) that mutate the `Simulation.status`. The loop reads
  status at safe points between ticks, so a pause never tears a tick in half. Terminal
  transitions emit `SimulationPaused|Resumed|Stopped|Completed` (canon §7).
- **Decoupled from HTTP.** The starting request returns immediately once the simulation is
  scheduled; the run proceeds on the background worker. Clients observe progress via
  `SimulationHub` (canon §8) — consistent with the backend-as-source-of-truth rule
  ([ADR-006](./ADR-006-backend-source-of-truth.md)).
- **Behind ports.** The worker depends on Application ports (event sink, scenario/simulation
  repositories, messaging adapters) and never on transport or persistence concretes
  directly, preserving the dependency rule (canon §3).
- **Real-time by logical clock, not wall clock.** Tick cadence is configurable and can be
  driven as fast as possible for headless tests or throttled for human-paced playback;
  correctness depends on tick order, not on wall-clock timing, which is what makes runs
  deterministic and replayable.

## Alternatives

### Request-scoped synchronous execution
Run the whole simulation inside the `start` HTTP request and stream as it goes. **Rejected:**
ties a long-lived, stateful run to a single request/connection lifetime — a dropped
connection or request timeout would kill the simulation. It also blocks a server thread for
the entire run and offers no clean seam for `pause`/`resume`/`stop` arriving as separate
requests. This is the "business logic on the request thread" anti-pattern the golden rules
reject.

### External worker process / service
Move the engine into a separate deployable worker consuming a job queue. **Rejected for now
(MVP–V1):** it adds a process boundary, inter-process transport, and operational surface we
do not yet need. The `BackgroundService` runs in the same host as the Api and SignalR hub,
which keeps event emission → dispatch latency minimal and the Docker Compose topology simple
(`api` container; canon §2, [ADR-005](./ADR-005-docker-compose.md)). Extracting a worker
remains a clean future step because the runtime already sits behind Application ports — a
scaling decision, not a redesign.

### Message-broker-driven loop (self-scheduling via RabbitMQ/Kafka)
Drive ticks by having the engine publish/consume its own scheduling messages through the
real brokers. **Rejected:** it conflates the *subject* of the simulation (brokers we are
teaching about, canon §2) with the *mechanism* that runs it, making the timeline
non-deterministic (broker delivery timing) and replay unreliable — directly undermining
`sequence`/`tick` ordering ([ADR-009](./ADR-009-event-envelope-sequencing.md)). Broker
adapters model behavior *inside* a tick; they must not clock the engine.

### Client timer driving ticks
Let the browser tick the engine via periodic calls. **Rejected:** it hands the clock to the
least trustworthy, least available participant and violates backend-as-source-of-truth
([ADR-006](./ADR-006-backend-source-of-truth.md)). Multiple viewers of one simulation would
fight over the clock, and replay would be impossible.

## Consequences

### Positive
- **Deterministic, replayable timeline.** A logical-clock tick loop produces events in a
  fixed order, giving `sequence`/`tick` real meaning and enabling exact replay and V2
  timeline scrubbing (canon §14, [ADR-009](./ADR-009-event-envelope-sequencing.md)).
- **Clean lifecycle control.** `pause`/`resume`/`stop` are honored at tick boundaries, so
  the engine never observes a half-applied state and animations stay coherent.
- **Idiomatic and DI-native.** `BackgroundService` is the ASP.NET 8 primitive for hosted
  work (canon §2), wired through the composition root with the rest of the graph
  ([ADR-004](./ADR-004-clean-architecture.md)); no bespoke threading infrastructure.
- **Low emit-to-client latency.** Running in-process with the `SimulationHub` keeps the path
  from event emission to client render short.

### Negative
- **In-process scaling limits.** All active simulations share one host's resources; a single
  host caps concurrent runs. Mitigation: the runtime sits behind Application ports, so
  extracting an external worker or sharding by `simulationId` is an additive change, not a
  rewrite.
- **State lives in a long-lived worker.** A host restart interrupts in-flight runs.
  Mitigation: events are persisted with `sequence` (canon §10), so a simulation can be
  resumed/replayed from its last durable `sequence` rather than lost.
- **Cadence tuning required.** A too-fast loop can outrun clients or starve other work; too
  slow harms responsiveness. Mitigation: tick cadence is configurable and independent of
  correctness (which depends only on tick order), and SignalR batching absorbs bursts
  (canon §8).

## Related documents

- [Architecture](../02-architecture/architecture.md)
- [Event Model](../02-architecture/event-model.md)
- [Sequence Diagrams](../02-architecture/sequence-diagrams.md)
- [ADR-006: Backend source of truth](./ADR-006-backend-source-of-truth.md)
- [ADR-009: Event envelope & sequencing](./ADR-009-event-envelope-sequencing.md)
- [ADR-004: Clean Architecture](./ADR-004-clean-architecture.md)
- [ADR Index](./README.md)
