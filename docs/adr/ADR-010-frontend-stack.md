# ADR-010: Frontend stack — React 18 + TypeScript + Vite + Zustand + Tailwind + Framer Motion + SignalR

- **Status:** Accepted
- **Date:** 2026-07-07

## Context

The DFL web client is a single-page application whose job is to (a) let learners visually
compose architectures on a `Node`/`Edge` canvas and (b) render running `Simulation`s where
**every animation is driven by a backend `SimulationEvent`** and the client never invents
state (canon §1, [ADR-006](./ADR-006-backend-source-of-truth.md)). This shapes the frontend
requirements concretely:

- A **realtime, event-driven SPA** that subscribes to `SimulationHub` (canon §8) and folds a
  high-throughput event stream into presentation state, smoothly, at product quality
  (~50 fps on large scenarios; see [ADR-001](./ADR-001-react-flow.md)).
- **React 18 + TypeScript** as fixed by the canon and CLAUDE.md, with the `features/` layout
  (`catalog`, `canvas`, `simulation`, `inspector`) plus `realtime`, `state`, `domain`
  (canon §4), and **React Flow** for the canvas ([ADR-001](./ADR-001-react-flow.md)).
- **Strong typing across the wire** so the `domain/` TS types mirror the backend contracts —
  the event envelope (canon §6), `NodeType` (canon §5), DTOs — and drift is caught at compile
  time.
- Smooth **token/edge animation** decoupled from React re-render cost, and a lightweight
  client **state** model that treats the event stream as the input to a fold, not a place to
  originate domain facts.

The canon already fixes the toolchain (canon §2, §4). This ADR records adopting that stack as
a coherent whole and justifies each choice against its main rivals.

## Decision

We adopt the following frontend stack for the `web/` SPA (canon §2, §4):

- **React 18 + TypeScript** — the mandated component model, with concurrent-rendering
  features and end-to-end static typing against the `domain/` contract types.
- **Vite** — dev server and build tool, for fast HMR and lean production bundles.
- **React Flow** — the node/edge canvas (its own decision,
  [ADR-001](./ADR-001-react-flow.md)); listed here as the anchor the rest of the stack
  serves.
- **Zustand** — client state (canon §2, §4). Stores (`canvas`, `simulation`, `ui`) hold
  presentation-derived state; the `simulation` store reduces incoming `SimulationEvent`s into
  view state and synthesizes the frontend-only `AnimationStarted`/`AnimationFinished`
  presentation events (canon §7) — never domain state
  ([ADR-006](./ADR-006-backend-source-of-truth.md)).
- **Tailwind CSS** — styling plus the design-token layer (canon §2), keeping styles
  colocated with small, composable components (CLAUDE.md Frontend Principles).
- **Framer Motion** — animation of message tokens along edges and node state transitions,
  each animation a rendering of a backend event.
- **@microsoft/signalr** — the realtime transport client (canon §2, §8), living in
  `web/src/realtime/` and owning connection, subscription (`Subscribe`/`Unsubscribe`), and
  reconnection with `?fromSequence=` resync
  ([ADR-009](./ADR-009-event-envelope-sequencing.md)).
- **Vitest + React Testing Library + Playwright** — the frontend test tiers (canon §2, §4;
  [ADR-013](./ADR-013-testing-strategy.md)).

Together these keep the client a **thin, strongly typed renderer** over the backend event
stream.

## Alternatives

### Angular
A batteries-included framework. **Rejected:** the canon and CLAUDE.md mandate React, and
React Flow — our canvas foundation ([ADR-001](./ADR-001-react-flow.md)) — is a React-native
library. Angular's heavier framework model and DI/RxJS conventions add weight we do not need
for a canvas-centric SPA and would fight the React Flow + Framer Motion composition.

### Vue
A capable reactive SPA framework. **Rejected:** same mandate mismatch, and the richest
node-graph and animation ecosystems we depend on (React Flow, Framer Motion) are React-first.
Choosing Vue would mean weaker library fit for the exact surfaces that define the product.

### Next.js with SSR
React with server-side rendering and routing. **Rejected:** DFL is an authenticated,
highly-interactive realtime canvas app — there is no SEO or first-paint-of-content benefit
from SSR, and a persistent `SimulationHub` WebSocket plus a client-owned canvas make a plain
Vite SPA simpler to reason about and deploy (`web` container; canon §2,
[ADR-005](./ADR-005-docker-compose.md)). SSR would add server rendering complexity for no
gain here.

### Redux Toolkit instead of Zustand
The established React state library. **Rejected:** our state need is a compact fold of an
event stream into presentation state, not a large normalized global store with extensive
middleware. Zustand offers that with far less boilerplate and fewer re-render pitfalls, which
matters directly for the frame-rate budget ([ADR-001](./ADR-001-react-flow.md)). RTK's
ceremony would be over-engineering for this shape (CLAUDE.md).

### CSS-in-JS (styled-components / Emotion) instead of Tailwind
Runtime-styled components. **Rejected:** the canon fixes Tailwind as the styling and
design-token layer (canon §2), and utility classes avoid runtime style computation on a
canvas that re-renders frequently — a measurable win against the performance budget. A
runtime CSS-in-JS layer would add per-render cost exactly where we are most sensitive.

## Consequences

### Positive
- **Fast SPA developer experience.** Vite HMR, TypeScript, and Zustand's low ceremony keep
  iteration quick and the client small and legible (CLAUDE.md — small, reusable components).
- **Tight fit for the product's core.** React 18 + React Flow + Framer Motion compose
  naturally for an animated node/edge canvas ([ADR-001](./ADR-001-react-flow.md)), and
  `@microsoft/signalr` is the first-party client for the mandated transport (canon §8).
- **Type safety across the wire.** `domain/` TS types mirror the canonical envelope and DTOs
  (canon §4, §6), catching contract drift at compile time.
- **Client stays a renderer.** Zustand stores fold events into view state and never
  originate domain facts, structurally upholding the golden rule
  ([ADR-006](./ADR-006-backend-source-of-truth.md)).

### Negative
- **SPA trade-offs.** No SSR means no server-rendered first paint and client-side routing
  must handle deep links. Mitigation: acceptable for an authenticated realtime tool; the
  app shell in `web/src/app` loads fast via Vite code-splitting.
- **Assembled stack, not one framework.** We own the integration of several libraries rather
  than adopting one opinionated framework. Mitigation: each choice is canonical and
  mainstream, boundaries are clear (`realtime`, `state`, `features`), and versions are pinned
  and CI-tested.
- **Client-held state can drift from backend if misused.** A store *could* be coded to guess
  state. Mitigation: the architecture and [ADR-006](./ADR-006-backend-source-of-truth.md)
  forbid it, and tests assert the client renders only from received events
  ([ADR-013](./ADR-013-testing-strategy.md)).

## Related documents

- [Architecture](../02-architecture/architecture.md)
- [Components](../02-architecture/components.md)
- [Design System](../03-ui/design-system.md)
- [Animations](../03-ui/animations.md)
- [ADR-001: React Flow](./ADR-001-react-flow.md)
- [ADR-006: Backend source of truth](./ADR-006-backend-source-of-truth.md)
- [ADR Index](./README.md)
