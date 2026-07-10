# ADR-001: React Flow for the node/edge canvas

- **Status:** Accepted
- **Date:** 2026-07-07

## Context

The Distributed Flow Lab (DFL) canvas is the product's primary surface. Learners
compose an architecture from canonical `NodeType`s (`Producer`, `Consumer`, `Exchange`,
`Queue`, `Topic`, `Partition`, `Broker`, `Database`, `Cache`, `DeadLetterQueue`,
`ApiGateway`, `LoadBalancer`, `Service`, `Client`) and connect them with directed
`Edge`s. The same canvas then plays back a running `Simulation`: message tokens travel
along edges and node badges update, driven **exclusively** by backend `SimulationEvent`s
(canon §1, §6, §7).

The canvas must therefore satisfy a demanding set of requirements simultaneously:

- A first-class **node/edge graph model** that maps directly onto the domain `Node` and
  `Edge` entities (canon §10), including per-node and per-edge `config`.
- **Interactive editing** — drag to place, connect, select, multi-select, pan, zoom, and
  snap — with a low-friction developer API so we can build a custom node palette.
- **Fully custom node and edge rendering** so each `NodeType` has a distinct, teaching-
  oriented visual, and edges can host animated message tokens (Framer Motion, canon §2).
- **Smooth animation at scale.** The product-quality bar targets ~50 fps on a 500-node
  scenario (see `../01-product/vision.md`), so render and re-render cost must be
  controllable and localized to the nodes that changed.
- **React 18 + TypeScript + Vite** idioms: functional components, hooks, composition over
  inheritance, and strongly typed props (canon §2, CLAUDE.md Frontend Principles).

We evaluated a purpose-built graph/diagram library against building the canvas ourselves.

## Decision

We adopt **React Flow** (`@xyflow/react`) as the node/edge canvas library for the DFL web
SPA, used inside the `web/src/features/canvas` feature (canon §4).

React Flow is a React-native library whose core abstraction — a controlled collection of
nodes and edges — is a near one-to-one match for the DFL domain model. We use it as
follows:

- **Domain mapping.** Each domain `Node`/`Edge` maps to a React Flow node/edge; the
  domain `NodeType` selects a registered custom node component (`nodeTypes` map), and the
  domain `position{x,y}` is the React Flow node position. This keeps the frontend
  `domain/` types (canon §4) authoritative and the canvas a thin renderer over them.
- **Custom rendering.** Every `NodeType` gets a small, composable custom node component;
  edges use custom edge components that expose an SVG path along which Framer Motion
  animates message tokens in response to `MessagePublished` / `MessageEnqueued` /
  `MessageDequeued` / `AckReceived` etc.
- **State ownership.** The graph is a **controlled** component driven by the Zustand
  `canvas` store (canon §4). React Flow renders state; it does not become a second source
  of truth. Simulation playback mutates only presentation state derived from
  `SimulationEvent`s — consistent with the golden rule that the client never invents
  domain state.
- **Performance.** We rely on React Flow's virtualized rendering, memoized custom nodes,
  and the ability to update a single node's presentation without re-rendering the whole
  graph, to meet the frame-rate target on large scenarios.

## Alternatives

### D3 from scratch
Maximum control over rendering and physics. **Rejected:** D3 owns the DOM imperatively,
which fights React's declarative reconciliation. We would have to build node dragging,
connection handles, selection, pan/zoom, and a React integration layer ourselves — months
of undifferentiated work that React Flow already provides and maintains. High risk of
subtle re-render and lifecycle bugs.

### Cytoscape.js
Mature, high-performance graph library with excellent large-graph layout. **Rejected:** it
renders to its own canvas/WebGL surface outside the React tree, so per-node custom React
components and Framer Motion token animation on edges are awkward or impossible to express
idiomatically. Its strength (automatic graph analysis/layout of huge graphs) is not our
core need; our strength requirement (rich, individually styled, animatable React nodes) is
exactly where it is weakest.

### Custom SVG / Canvas
A hand-rolled SVG or Canvas engine. **Rejected:** same cost problem as D3 without even
D3's helpers. Canvas in particular would force us to reimplement hit-testing, accessibility,
and text layout. This is the definition of the "quick hack that becomes a maintenance sink"
the project philosophy forbids (CLAUDE.md).

### JointJS
Full-featured diagramming toolkit. **Rejected:** its commercial tier (Rappid) gates the
most useful features, its API is not React-native (it wraps its own MVC/Backbone-era
model), and integrating React component rendering per cell is against the grain. Licensing
and a non-React programming model make it a poor fit for a React 18 + TypeScript codebase.

## Consequences

### Positive
- The domain `Node`/`Edge` model maps directly onto the library's model, minimizing
  translation logic and keeping the Zustand store authoritative.
- Editing affordances (drag, connect, select, pan, zoom) come for free and behave the way
  users expect, letting us invest effort in teaching-specific visuals instead.
- Custom React node/edge components compose cleanly with Framer Motion, so animations
  remain pure renderings of backend events.
- Strong TypeScript support aligns with the canonical stack and catches contract drift at
  compile time.

### Negative
- We inherit React Flow's rendering model and upgrade cadence; a breaking major version
  (e.g. the `react-flow-renderer` → `@xyflow/react` rename) requires a coordinated
  migration. Mitigation: isolate all React Flow usage inside `features/canvas` behind our
  own node/edge component wrappers.
- Extreme-scale graphs (well beyond the 500-node target) may still require manual
  virtualization tuning; React Flow is DOM/SVG-based rather than WebGL. Mitigation: the
  performance budget is defined and testable via Playwright, and node components are
  memoized from the start.
- A third-party dependency sits on the critical product path. Mitigation: it is MIT-
  licensed, widely adopted, and actively maintained, and our wrapper boundary keeps it
  replaceable.

## Related documents

- [Architecture](../02-architecture/architecture.md)
- [Event Model](../02-architecture/event-model.md)
- [Container Diagram](../diagrams/container-diagram.md)
- [Message Flow](../diagrams/message-flow.md)
- [ADR Index](./README.md)
- [Product Vision](../01-product/vision.md)
