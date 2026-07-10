# ADR-002: SignalR for realtime backend→frontend event streaming

- **Status:** Accepted
- **Date:** 2026-07-07

## Context

DFL's defining constraint is that **every frontend animation is driven by a real backend
event**; the client never invents state (canon §1). The Simulation Engine
(`BackgroundService` tick loop) emits an ordered stream of `SimulationEvent`s wrapped in
the canonical envelope (canon §6) — `MessagePublished`, `MessageRouted`, `AckReceived`,
`DeadLettered`, `SagaStepCompleted`, and the rest of the Event Catalog (canon §7). Those
events must reach the browser with low latency so that playback feels live.

Requirements for the transport:

- **Server-push, one-directional-dominant** delivery: the server pushes many events per
  simulation to subscribed clients; the client's only outbound needs are lightweight
  control calls (`Subscribe` / `Unsubscribe`).
- **Fan-out by simulation.** Many clients may watch the same `Simulation`; delivery must be
  scoped to only the connections observing a given `simulationId` (canon §8 groups).
- **Ordering & throughput.** Events carry a monotonic `sequence`; the client detects gaps.
  High-throughput simulations require **batched** delivery.
- **Resilience.** Connections drop; the client must reconnect and resynchronize (via
  `GET /api/v1/simulations/{id}/events?fromSequence=`, canon §9).
- **Horizontal scale.** With multiple stateless API instances, an event produced on one
  instance must reach clients connected to another — i.e. a backplane (see
  `../02-architecture/architecture.md` §7).
- **Fit the stack.** The backend is ASP.NET 8; the frontend already standardizes on
  `@microsoft/signalr` (canon §2).

The realtime hub contract is fixed by canon §8: hub `SimulationHub` mapped at
`/hubs/simulation`; server→client `ReceiveSimulationEvent`, `ReceiveSimulationEvents`
(batched), `SimulationStateChanged`; client→server `Subscribe(simulationId)` /
`Unsubscribe(simulationId)`; one SignalR **group per `simulationId`**.

## Decision

We adopt **ASP.NET SignalR** as the realtime transport, exposed as the `SimulationHub` at
`/hubs/simulation` (canon §8), with the `@microsoft/signalr` client in the web SPA's
`realtime/` module (canon §4).

Concretely:

- The Infrastructure layer's SignalR event dispatcher (canon §3) publishes each
  `SimulationEvent` to the SignalR group named by its `simulationId`, invoking
  `ReceiveSimulationEvent` (single) or `ReceiveSimulationEvents` (batched) on clients.
- Lifecycle/status transitions push `SimulationStateChanged(SimulationStateDto)`.
- Clients call `Subscribe(simulationId)` to join the group and `Unsubscribe(simulationId)`
  to leave; group membership scopes fan-out to exactly the interested connections.
- In multi-instance deployments SignalR uses a **Redis backplane** (Redis pub/sub, canon §2)
  so events fan out across API instances.
- SignalR negotiates the best available transport (**WebSockets** first, falling back to
  Server-Sent Events, then long polling) transparently, and provides automatic reconnection
  in the client. On reconnect the client resynchronizes gaps via the REST replay endpoint
  using the last observed `sequence`.

## Alternatives

### Raw WebSockets
The transport SignalR itself prefers. **Rejected as the direct API:** choosing raw
WebSockets means hand-building connection lifecycle, automatic reconnection, transport
fallback for restrictive networks, group/broadcast semantics, a multi-server backplane,
and a hub-method RPC convention — all of which SignalR provides out of the box and which
exactly match canon §8's method contract. We would rebuild SignalR, worse. SignalR still
uses WebSockets under the hood when available, so we lose nothing on the happy path.

### Server-Sent Events (SSE)
A simple, HTTP-native server-push mechanism — a good conceptual fit for a mostly one-way
event stream. **Rejected:** SSE is unidirectional (no clean client→server channel for
`Subscribe`/`Unsubscribe`), text/UTF-8 only, capped by browser per-host connection limits,
and has no built-in grouping or multi-server fan-out. We would still need a separate
channel for control calls and a bespoke backplane. SignalR already includes SSE as a
fallback transport, so we keep the benefit without the limitations as our primary API.

### Polling (periodic REST)
The client repeatedly calls `GET /api/v1/simulations/{id}/events?fromSequence=`.
**Rejected for the live path:** polling trades latency for request volume — meeting the
P95 event-to-animation target of ≤250 ms (see `../01-product/vision.md`) would require
aggressive intervals and enormous request overhead, and playback would feel choppy rather
than live. Note the same endpoint is retained deliberately for **history/replay and
reconnect resynchronization**, which is where pull semantics are the right tool.

### gRPC-web streaming
Server-streaming gRPC over gRPC-web. **Rejected:** gRPC-web still cannot do true
bidirectional streaming in the browser, adds a proxy (Envoy/gRPC-web translation) to the
deployment, and introduces a second IDL/serialization stack (protobuf) alongside our JSON
camelCase envelope (canon §6). The operational and cognitive cost is not justified when
SignalR already satisfies every requirement natively on an ASP.NET 8 backend.

## Consequences

### Positive
- The canon §8 hub contract (`ReceiveSimulationEvent`, `ReceiveSimulationEvents`,
  `SimulationStateChanged`, `Subscribe`, `Unsubscribe`) maps directly onto SignalR hub
  methods and typed clients — minimal glue code.
- Transport negotiation and automatic reconnection are handled by the library, improving
  reliability across corporate proxies and flaky networks.
- Group-per-`simulationId` gives precise, cheap fan-out; the Redis backplane makes the API
  horizontally scalable without changing the contract.
- First-class ASP.NET 8 and `@microsoft/signalr` integration keeps us on the canonical
  stack with typed hubs on both ends.

### Negative
- SignalR is a Microsoft-ecosystem abstraction; a future non-.NET service consuming the
  same stream would need the SignalR protocol or a bridge. Mitigation: the event envelope
  is plain JSON and the REST replay endpoint offers a transport-neutral fallback.
- The Redis backplane becomes shared infrastructure whose availability affects multi-
  instance fan-out. Mitigation: single-instance dev needs no backplane; Redis is already in
  the stack for `Cache` nodes.
- Batching (`ReceiveSimulationEvents`) adds client-side reassembly/ordering logic to honor
  `sequence`. Mitigation: gap detection and replay-on-gap are specified in the realtime
  module and covered by tests.

## Related documents

- [Architecture](../02-architecture/architecture.md)
- [Event Model](../02-architecture/event-model.md)
- [Message Flow](../diagrams/message-flow.md)
- [Container Diagram](../diagrams/container-diagram.md)
- [ADR Index](./README.md)
