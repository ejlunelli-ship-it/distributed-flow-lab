# Execution Roadmap

This document sequences the **engineering execution** of Distributed Flow Lab into
vertical, testable increments. It complements — and never overrides — the canonical
**product** roadmap ([`../01-product/roadmap.md`](../01-product/roadmap.md)) and the
[Backlog](../01-product/backlog.md). Where the product roadmap fixes *what* ships in each
phase (MVP → V1 → V2 → V3 → Future), this document orders *how* the MVP is built, sprint by
sprint, so each step compiles, is tested, and leaves the tree green.

Each sprint ends by running the test gates and synchronizing documentation (backlog,
roadmap, ADRs), per [`../../CLAUDE.md`](../../CLAUDE.md).

## MVP sprints

| Sprint | Deliverable | Backlog epics | Exit criteria |
|--------|-------------|---------------|---------------|
| **S0 — Scaffolding** ✅ | .NET solution (4 layers + 3 test projects), Vite SPA, Docker Compose (`web`,`api`,`rabbitmq`,`postgres`), GitHub Actions CI, `Directory.Build.props` (nullable, warnings-as-errors) | Epic 7 (P0) | `dotnet build`/`npm run build` green; API `GET /health` responds; Docker images build; CI configured |
| **S1 — Event core** ✅ | Domain (`NodeType`, `SimulationStatus`, entities, envelope), Event Catalog, engine `BackgroundService` with tick clock + monotonic `sequence`, lifecycle commands via MediatR, `/api/v1` scenario + simulation endpoints (in-memory persistence), RFC 7807 errors | Epic 2 (P0) | Engine emits `TickAdvanced` + lifecycle events in order; identical event streams across runs (xUnit, 35 tests); live REST flow verified end-to-end |
| **S2 — Realtime streaming** ✅ | `SimulationHub` (per-`simulationId` groups) + `SignalREventPublisher` replacing the null transport, `ReceiveSimulationEvent` / `SimulationStateChanged` pushes, SPA realtime client (auto-reconnect + resubscribe) and Zustand `simulationStore` with strict sequence ordering, dup-drop and gap buffering, live dev panel | Epic 4 (P0) | SignalR contract tests green (events in `sequence` order, group isolation); live verification: gap-free stream, emit→client P95 = 17 ms (NFR-1 ≤ 250 ms) |
| **S3 — Canvas editor** | React Flow shell (pan/zoom), `NodeType` palette, edge creation + type-rule validation, node/edge inspector config | Epic 1 (P0) | User composes `Producer→Exchange→Queue→Consumer`; invalid edges rejected inline |
| **S4 — RabbitMQ + REST semantics** | RabbitMQ adapter (exchanges/queues/routing keys/DLX) + node behaviors; full `MessagePublished→…→AckReceived` chain; HTTP semantics (`HttpRequestStarted/…`) | Epics 2, 3 (P0) | AMQP events match real broker rules (Testcontainers); HTTP events correct |
| **S5 — Animation + inspector + persistence** | Framer Motion message tokens mapped to `eventId`, client-only `AnimationStarted/Finished`, live timeline inspector, EF Core timeline persistence, backfill `?fromSequence=` | Epics 6, 7 (P0) | All five MVP exit criteria in the product roadmap met |

## After the MVP

The canonical product phases continue:

- **Version 1** — Kafka, Redis cache, retry/DLQ, metrics dashboard, scenario save/load, catalog.
  Adds `kafka` + `redis` to the Compose stack and EF Core persistence for scenario/simulation
  metadata.
- **Version 2** — CQRS, Saga, Circuit Breaker, gRPC, fault-injection UI, timeline scrubbing/replay.
- **Version 3** — Event Sourcing, API Gateway, multi-user classrooms, guided lessons, assessments.
- **Future** — collaboration, plugin SDK, marketplace, cloud multi-tenant SaaS.

## Conventions per sprint

1. Implement one backlog feature at a time; explain the plan before coding.
2. Add tests (unit first; integration via Testcontainers where a broker/DB is involved).
3. Keep the backend warnings-as-errors clean and the SPA type-checked, linted, and formatted.
4. Update the backlog/roadmap and add an ADR when an architectural decision is made.

## Related documents

- [Product Roadmap](../01-product/roadmap.md)
- [Backlog](../01-product/backlog.md)
- [Folder Structure](./folder-structure.md)
- [Technologies](./technologies.md)
- [Testing](./testing.md)
- [ADR-014: Sprint 0 scaffolding](../adr/ADR-014-scaffolding-toolchain-reality.md)
- [ADR-015: OSS dependency pinning](../adr/ADR-015-oss-dependency-pinning.md)
- [Architecture](../02-architecture/architecture.md)
