# ADR-016: Curated node-type connection rules and a store-owned canvas

- **Status:** Accepted
- **Date:** 2026-07-13

## Context

Sprint 3 delivers the canvas editor (Epic 1): learners drag canonical `NodeType`s onto a
React Flow surface and connect them with directed `Edge`s. Two decisions had to be made that
the documentation canon did not already fix.

1. **Which connections are legal?** The backlog states only one illustrative rule — a
   `Consumer → Producer` edge is illegal (Epic 1) — and no document defines a full
   `NodeType → NodeType` legality matrix. Yet the product's whole purpose is to teach
   *correct* distributed topologies, so the editor must reject wiring that would mislead a
   learner, and must explain *why* inline (the Epic 1 acceptance criterion: "invalid links are
   rejected with an inline message").

2. **Who owns the canvas state?** [ADR-001](./ADR-001-react-flow.md) already decided that React
   Flow renders a *controlled* graph and must not become a second source of truth, mirroring
   the golden rule ([ADR-006](./ADR-006-backend-source-of-truth.md)). This ADR records the
   concrete client-side shape of that ownership and how node configuration stays maintainable
   across 14 node types.

## Decision

### 1. A curated, directional connection matrix

`web/src/domain/connectionRules.ts` defines `ALLOWED_TARGETS: Record<NodeType, NodeType[]>` — a
directional source→target matrix — and a pure `validateConnection(source, target)` that returns
`{ valid, reason? }`. A `source → target` edge is legal iff `target`'s type is in
`ALLOWED_TARGETS[source]`; otherwise the verdict carries a human, educational reason (e.g.
"A Consumer cannot connect to a Producer.", or a terminal-node message for storage sinks).

The matrix encodes the messaging/request-flow semantics the platform teaches: actors
(`Producer`, `Client`) originate flow; `Service` compute calls downstream and publishes to
messaging infrastructure; RabbitMQ `Exchange → Queue/DeadLetterQueue` and Kafka
`Broker → Topic → Partition → Consumer` follow real broker topologies; `Consumer` is a sink
(it may persist to `Database`/`Cache` or call a `Service`, but never publishes back to a
`Producer`); `Database`/`Cache` are terminal. The module is pure and reasons only about
`NodeType`s, so it is unit-tested in isolation; **identity** checks (self-loop, duplicate edge)
belong to the store, which alone knows node identity.

### 2. The Zustand `canvasStore` is the authoritative canvas state

`web/src/state/canvasStore.ts` owns `nodes`, `edges`, selection, and a `dirty` flag. React Flow
is wired to it (`onNodesChange`/`onEdgesChange`/`onConnect`) and only renders it. `connect()`
runs the identity checks and `validateConnection` before adding an edge and returns the verdict;
the canvas surfaces the reason inline via `uiStore.connectionError`. React Flow never holds
state the store does not.

### 3. Node metadata and config are data-driven

`web/src/domain/nodeCatalog.ts` (`NODE_CATALOG`) is the single declarative description of every
`NodeType`: its palette family and its configurable fields (`NodeConfigField[]`). The palette,
the default `config` of a dropped node, the inspector's config form, and client-side validation
all derive from this one table. Adding or changing a node type is a data edit here — never a
`switch` spread across components (OCP applied on the client). One memoized `FlowNode` renders
every variant, tinted by the `var(--node-<type>)` design token (design-system.md §2.2).

## Alternatives

### Permissive / no type rules (allow any edge)
Only reject self-loops and duplicates. **Rejected:** it lets learners build nonsensical
topologies (e.g. `Database → Producer`) with no feedback, defeating the educational goal and the
Epic 1 acceptance criterion.

### Let React Flow own the graph state
Use React Flow's internal state as the source and read it back on demand. **Rejected:** it
violates [ADR-001](./ADR-001-react-flow.md) and [ADR-006](./ADR-006-backend-source-of-truth.md)
— the store must be authoritative so Run-mode animation and (later) persistence fold over a
single, controlled model.

### A bespoke config form component per node type
Fourteen hand-written forms. **Rejected:** near-identical duplication that drifts from the
domain and scales badly (CLAUDE.md — no duplication, avoid overengineering). The data-driven
catalog gives the same UX with one form renderer.

### Encode the matrix as edge-target rules inside React Flow's `isValidConnection`
Push validation entirely into the React Flow callback. **Rejected:** the rule would then be
framework-coupled and hard to unit-test and to share with future backend scenario validation.
Keeping it a pure domain function preserves testability and portability; the callback simply
delegates.

## Consequences

### Positive
- **The editor teaches.** Illegal wiring is refused with a concrete, human reason, turning a
  mistake into a lesson.
- **Single source of truth upheld.** The store owns topology; React Flow renders it — structural
  consistency with ADR-001/ADR-006, and a clean foundation for Run-mode animation (Sprint 5).
- **Extensible by data.** New node types and connection rules are table edits, unit-tested
  without touching components.
- **Testable rules.** `validateConnection` and the catalog are pure and covered by fast unit
  tests; the compose flow is proven end-to-end by a Playwright spec.

### Negative
- **The matrix is a curated judgement, not canon.** It may need revision as new patterns
  (CQRS, Saga, API Gateway fan-out) arrive in later phases. Mitigation: it lives in one pure
  module with tests; changes are localized and reviewable.
- **Client/server duplication risk.** When server-side scenario validation is added, the rules
  must be mirrored. Mitigation: the module is small and self-contained, and this ADR flags the
  need to keep the two in sync.

## Related documents

- [ADR-001: React Flow for the node/edge canvas](./ADR-001-react-flow.md)
- [ADR-006: Backend simulation engine as the single source of truth](./ADR-006-backend-source-of-truth.md)
- [ADR-010: Frontend stack](./ADR-010-frontend-stack.md)
- [Data Model](../02-architecture/data-model.md)
- [Components](../03-ui/components.md)
- [Wireframes](../03-ui/wireframes.md)
- [Execution Roadmap](../05-dev/execution-roadmap.md)
- [ADR Index](./README.md)
