# ADR-009: Canonical event envelope with monotonic sequencing

- **Status:** Accepted
- **Date:** 2026-07-07

## Context

The `Timeline` is defined as the **ordered, replayable sequence of `SimulationEvent`s** for
a `Simulation` (canon §5), and the backend engine is its single source of truth
([ADR-006](./ADR-006-backend-source-of-truth.md)). Every animation the learner sees is a
rendering of one of these events, so their **order and completeness** are not cosmetic — a
reordered or missing event mis-teaches how a distributed system behaves.

The events travel over two independent paths that must agree:

- **Live push** over `SimulationHub`, including a batched
  `ReceiveSimulationEvents(SimulationEventEnvelope[])` for high throughput (canon §8), where
  transport can coalesce, batch, or (on reconnect) drop deliveries.
- **History / replay** via `GET /api/v1/simulations/{id}/events?fromSequence=` (canon §9),
  which must return exactly the same events in exactly the same order.

Wall-clock time alone cannot order these: many events share the same `Tick`, batches carry
several events, and `occurredAt` has limited resolution. The client needs a way to (a) place
events in a total order, (b) detect that it missed some, and (c) resume precisely from where
it left off. The canon already defines the envelope and the two clock fields for exactly this
(canon §6). This ADR records adopting that envelope with a **monotonic per-simulation
`sequence`** as the ordering and replay contract.

## Decision

Every `SimulationEvent` is transported in the **canonical envelope** (canon §6, JSON,
camelCase on the wire), and ordering/replay are governed by two fields with distinct roles:

```json
{
  "eventId": "d1f...-guid",
  "simulationId": "a90...-guid",
  "sequence": 42,
  "tick": 17,
  "occurredAt": "2026-07-07T15:30:00.123Z",
  "type": "MessagePublished",
  "sourceNodeId": "node-producer-1",
  "targetNodeId": "node-exchange-1",
  "correlationId": "msg-8f3...-guid",
  "traceId": "trace-2b1...-guid",
  "payload": { "routingKey": "order.created", "sizeBytes": 512 }
}
```

- **`sequence` — the total order.** A **monotonic, gap-free, per-`simulationId`** integer
  assigned by the engine as it appends each event to the `Timeline`. It is *the* ordering
  key: clients sort by `sequence`, and a jump greater than one signals a gap
  (missed/out-of-order delivery), triggering a resync.
- **`tick` — the logical simulation clock.** The engine `Tick` (canon §5) at which the event
  occurred, from the `BackgroundService` loop
  ([ADR-007](./ADR-007-background-service-engine.md)). Many events can share one `tick`;
  `sequence` breaks ties within and across ticks. `tick` groups events for timeline display
  and scrubbing (canon §14); `sequence` orders them.
- **`occurredAt`** is wall-clock ISO-8601 UTC for human display and diagnostics only — never
  the ordering key.
- **Gap detection & resync.** A client tracks the highest `sequence` it has applied. On
  reconnect or on detecting a gap, it calls
  `GET /api/v1/simulations/{id}/events?fromSequence={last}` (canon §9) to fetch the missing
  suffix, then resumes the live stream — reconciling the two delivery paths into one
  authoritative order.
- **Persistence.** Each event is persisted with its `sequence` and `simulationId` (canon
  §10) so the history endpoint replays the identical ordered stream long after the run ends.
- **Correlation.** `correlationId` links message-related events to a `messageId`, and
  `traceId` ties them to distributed traces
  ([ADR-012](./ADR-012-observability-opentelemetry.md)); `targetNodeId` is null for
  node-local events (canon §6).

The envelope is **uniform across all event types** (canon §7): every event — lifecycle,
messaging, resilience, fault injection — carries the same envelope, so consumers have one
parsing and ordering contract regardless of `type`.

## Alternatives

### Unordered events (rely on delivery order)
Emit events with no ordering field and trust the transport to deliver them in order.
**Rejected:** SignalR batching (`ReceiveSimulationEvents`, canon §8), reconnects, and the
separate replay path make delivery order unreliable. Without `sequence` the client cannot
distinguish "nothing happened" from "I missed events", so it could silently render a
corrupted timeline — the exact fidelity failure the golden rules forbid
([ADR-006](./ADR-006-backend-source-of-truth.md)).

### Wall-clock ordering only (`occurredAt`)
Order strictly by timestamp. **Rejected:** many events share a `tick` and thus near-identical
timestamps; millisecond resolution cannot totally order them, and clock granularity/skew
across batches produces ambiguous or unstable orderings. Replay would not be guaranteed
byte-for-byte identical to the live run. Wall-clock is kept for display, not ordering.

### Per-type sequences (a counter per event `type`)
Number `MessagePublished`, `AckReceived`, etc. independently. **Rejected:** it makes each
type internally ordered but provides **no cross-type order** — the client could not know
whether a `MessageEnqueued` preceded its `DeadLettered`, which is the whole point. A single
per-simulation `sequence` gives one unambiguous global order; `tick` and `type` remain
available for grouping/filtering.

### Vector clocks / causal metadata per event
Attach causal ordering metadata. **Rejected as over-engineering:** the engine is the sole
producer of a simulation's timeline ([ADR-006](./ADR-006-backend-source-of-truth.md)), so a
single monotonic counter already yields a correct total order. Vector clocks solve
multi-producer causality we do not have, at real payload and complexity cost (CLAUDE.md
"avoid overengineering").

## Consequences

### Positive
- **Reliable total order and replay.** Sorting by `sequence` reconstructs the exact
  `Timeline` on any client, live or historical — the foundation for V2 scrubbing/replay
  (canon §14).
- **Gap detection built in.** A monotonic, gap-free counter lets clients notice missed
  events and resync precisely via `?fromSequence=` (canon §9), reconciling live push and
  history into one truth.
- **One uniform contract.** A single envelope for all event types (canon §6, §7) means one
  parser, one ordering rule, and one place for `correlationId`/`traceId` propagation
  ([ADR-012](./ADR-012-observability-opentelemetry.md)).
- **Clean separation of clocks.** `sequence` orders, `tick` groups, `occurredAt` displays —
  no field is overloaded.

### Negative
- **Producer coordination cost.** Assigning a gap-free monotonic `sequence` requires the
  engine to be the serialization point per `simulationId`. Mitigation: the single-writer
  `BackgroundService` tick loop ([ADR-007](./ADR-007-background-service-engine.md)) already
  owns that per-simulation, making assignment trivial and contention-free.
- **Sequence state must survive restarts.** Resuming a persisted simulation must continue
  the counter without reuse or gaps. Mitigation: `sequence` is persisted with each event
  (canon §10); the engine resumes from the maximum stored value.
- **Slightly larger payloads.** Every event carries the full envelope. Mitigation: fields are
  small, batching amortizes overhead (canon §8), and the uniform shape is worth far more than
  the bytes saved by trimming it.

## Related documents

- [Event Model](../02-architecture/event-model.md)
- [WebSocket Events](../02-architecture/websocket-events.md)
- [API Contracts](../02-architecture/api-contracts.md)
- [Data Model](../02-architecture/data-model.md)
- [ADR-006: Backend source of truth](./ADR-006-backend-source-of-truth.md)
- [ADR-007: Background service engine](./ADR-007-background-service-engine.md)
- [ADR Index](./README.md)
