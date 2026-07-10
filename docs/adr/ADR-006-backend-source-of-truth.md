# ADR-006: Backend simulation engine as the single source of truth

- **Status:** Accepted
- **Date:** 2026-07-07

## Context

The defining promise of the Distributed Flow Lab (DFL) is that **every animation is driven
by a real backend event** — the frontend never invents state (canon §0, §1; CLAUDE.md
Event Driven Architecture). This is not a stylistic preference: it is the product's
educational contract. When a learner watches a `Message` travel from a `Producer` to an
`Exchange`, get routed to a `Queue`, and later be `DeadLettered`, they must be seeing the
**actual** behavior the backend simulation engine computed, not a plausible-looking
client-side approximation. If the animation and the truth ever diverge, the platform
teaches something false.

The core of the system is the Simulation Engine, whose job is to produce an authoritative,
ordered, replayable stream of `SimulationEvent`s (canon §6, §7). The engine already owns
the domain rules: how a `Node` reacts to a `Message`, which events it emits, in what order,
and at which `Tick`. The open question this ADR settles is **where simulation state lives**
and **who is allowed to compute it**.

The canon draws a hard line between two categories of event:

- **Domain events** — the ~50 canonical `SimulationEvent` types (canon §7), each carrying
  the full envelope with `sequence` and `tick` (canon §6). These are produced **only** by
  the backend engine and are the unit of truth for the `Timeline`.
- **Frontend-only presentation events** — `AnimationStarted` and `AnimationFinished`
  (canon §7). These describe the *rendering* of a backend event on the canvas; they never
  represent new domain state and never flow back to the server.

This ADR records why the backend is the exclusive authority for domain state, and why the
frontend is constrained to rendering.

## Decision

The **backend simulation engine is the single, exclusive source of truth** for all
simulation domain state. The frontend is a **pure renderer** of backend `SimulationEvent`s.

Concretely:

- **The engine computes; the client renders.** All domain state transitions — message
  routing, enqueue/dequeue, acks, retries, dead-lettering, circuit-breaker transitions,
  saga compensation, cache hits/misses, fault effects — are decided by the engine in the
  `DistributedFlowLab.Infrastructure` runtime and expressed as canonical
  `SimulationEvent`s. The web SPA subscribes to those events over `SimulationHub`
  (canon §8) and animates them via Framer Motion inside `features/simulation` and
  `features/canvas` (canon §4).
- **The client holds only presentation-derived state.** The Zustand `simulation` store
  (canon §4) reduces the incoming event stream into view state (token positions, node
  badges, timeline cursor). It may synthesize the frontend-only `AnimationStarted` /
  `AnimationFinished` events (canon §7) to sequence its own animations, but it never
  fabricates, guesses, or extrapolates a domain fact the engine has not emitted.
- **The wire carries backend events, not intentions.** The client sends **commands**
  (start/pause/resume/stop, inject fault — canon §9) and receives **events**. It never
  pushes computed state to the server; there is no "the client thinks the queue now has 3
  messages" path.
- **Replayable by construction.** Because state is a fold over an ordered event stream,
  the same `Timeline` fetched via `GET /api/v1/simulations/{id}/events?fromSequence=`
  (canon §9) reproduces the exact same visualization on any client. Determinism is a
  property of the engine (see [ADR-007](./ADR-007-background-service-engine.md)), and the
  client is a deterministic function of the events it receives.

This makes the golden rule (canon §1) structural rather than aspirational: the client has
no code path that can invent domain state, because it has no access to the domain rules.

## Alternatives

### Client-side simulation
Run the whole simulation in the browser (TypeScript engine) and treat the backend purely
as storage. **Rejected:** it collapses the educational contract. Broker fidelity —
RabbitMQ routing/DLX semantics, Kafka partitions/offsets, Redis eviction (canon §2) —
cannot be honestly reproduced in a browser reimplementation, so learners would study a
toy, not the real behavior the platform promises. It also forks the domain logic into two
languages, guaranteeing drift, and contradicts Clean Architecture, which keeps the domain
in `DistributedFlowLab.Domain` (canon §3, [ADR-004](./ADR-004-clean-architecture.md)).

### Optimistic client prediction
Let the client predict the next state locally for instant feedback and reconcile when the
authoritative event arrives. **Rejected:** prediction means the client sometimes shows
state the engine did not produce — precisely the "frontend invents state" failure the
golden rule forbids. Reconciliation flicker (a predicted `AckReceived` corrected to a
`MessageNacked`) would actively mis-teach failure behavior, which is core subject matter,
not an edge case. The complexity of a rollback/reconciliation layer is unjustified when the
SignalR round-trip is already sub-frame for typical simulations.

### Hybrid (client simulates "cosmetic" motion, backend owns "important" state)
Split responsibility: the client freely animates in-between motion, the backend owns
milestone state. **Rejected:** the line between "cosmetic" and "important" is exactly where
misconceptions are taught. Whether a token visibly waits in a `Queue` before a
`MessageDequeued`, or how long a `LatencyInjected` delay lasts, *is* the lesson. A hybrid
also reintroduces two sources of truth by the back door and makes replay non-deterministic
across clients. We keep a clean split instead: backend owns **all** domain state; client
owns **only** rendering, including its own `AnimationStarted`/`AnimationFinished` pacing.

## Consequences

### Positive
- **Fidelity and honesty.** What the learner sees is what the engine (and the real broker
  adapters) actually did. The platform teaches truth, satisfying the educational contract
  (canon §0) and the golden rule (canon §1).
- **Determinism and shared reality.** State is a fold over an ordered event stream, so
  every client — live via `SimulationHub` or replayed via the events endpoint — renders an
  identical `Timeline`. This underpins scrubbing/replay (roadmap V2, canon §14).
- **One place for domain logic.** Rules live only in `DistributedFlowLab.Domain` /
  `Application`; the client cannot duplicate or contradict them, upholding "never duplicate
  logic" (CLAUDE.md) and Clean Architecture ([ADR-004](./ADR-004-clean-architecture.md)).
- **Testability.** Because the client is a pure function of events, it can be tested with
  recorded event fixtures, and the engine can be tested headlessly (see
  [ADR-013](./ADR-013-testing-strategy.md)).

### Negative
- **Added latency and round-trips.** Every domain transition crosses the network before it
  animates. Mitigation: SignalR batching via `ReceiveSimulationEvents` (canon §8) and the
  engine's logical `Tick` clock decouple perceived smoothness from wall-clock jitter; the
  client can interpolate *motion between* two authoritative events without inventing state.
- **Backend liveness is on the critical path.** If the connection drops mid-simulation the
  canvas cannot advance on its own. Mitigation: SignalR reconnection plus resync via
  `GET /api/v1/simulations/{id}/events?fromSequence=` (canon §9) restores the exact
  timeline from the last received `sequence`.
- **More upfront event modelling.** Every visible behavior must have a corresponding
  canonical event, so the Event Catalog (canon §7) must stay ahead of the UI. Mitigation:
  the catalog is already authoritative and versioned in the canon; new visuals require a
  named event rather than a client shortcut.

## Related documents

- [Architecture](../02-architecture/architecture.md)
- [Event Model](../02-architecture/event-model.md)
- [WebSocket Events](../02-architecture/websocket-events.md)
- [Message Flow](../diagrams/message-flow.md)
- [ADR-007: Background service engine](./ADR-007-background-service-engine.md)
- [ADR-004: Clean Architecture](./ADR-004-clean-architecture.md)
- [ADR Index](./README.md)
