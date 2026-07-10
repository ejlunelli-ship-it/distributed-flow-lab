# Architecture Decision Records (ADR)

This directory holds the **Architecture Decision Records** for the Distributed Flow Lab
(DFL). An ADR captures a single significant architectural decision — its context, the
decision itself, the alternatives that were considered and rejected, and the resulting
consequences — so that the *reasoning* behind the system, not only its current shape, is
preserved for future engineers.

All ADRs conform to the shared [Documentation Canon](../../CLAUDE.md): the same ubiquitous
language, `NodeType`s, event names, contracts, and technology choices are used everywhere,
and every document ends with a **Related documents** section.

## Index

| ADR | Title | Status | Date |
|-----|-------|--------|------|
| [ADR-001](./ADR-001-react-flow.md) | React Flow for the node/edge canvas | Accepted | 2026-07-07 |
| [ADR-002](./ADR-002-signalr.md) | SignalR for realtime backend→frontend event streaming | Accepted | 2026-07-07 |
| [ADR-003](./ADR-003-rabbitmq.md) | Real RabbitMQ as a messaging adapter for fidelity | Accepted | 2026-07-07 |
| [ADR-004](./ADR-004-clean-architecture.md) | Clean Architecture layering for the backend | Accepted | 2026-07-07 |
| [ADR-005](./ADR-005-docker-compose.md) | Docker Compose for local development and orchestration | Accepted | 2026-07-07 |
| [ADR-006](./ADR-006-backend-source-of-truth.md) | Backend simulation engine as the single source of truth | Accepted | 2026-07-07 |
| [ADR-007](./ADR-007-background-service-engine.md) | Simulation runtime as an ASP.NET BackgroundService tick loop | Accepted | 2026-07-07 |
| [ADR-008](./ADR-008-cqrs-mediatr.md) | CQRS via MediatR in the Application layer | Accepted | 2026-07-07 |
| [ADR-009](./ADR-009-event-envelope-sequencing.md) | Canonical event envelope with monotonic sequencing | Accepted | 2026-07-07 |
| [ADR-010](./ADR-010-frontend-stack.md) | Frontend stack — React 18, TypeScript, Vite, Zustand, Tailwind, Framer Motion, SignalR | Accepted | 2026-07-07 |
| [ADR-011](./ADR-011-postgres-efcore.md) | PostgreSQL via EF Core for persistence | Accepted | 2026-07-07 |
| [ADR-012](./ADR-012-observability-opentelemetry.md) | Observability with OpenTelemetry and Serilog | Accepted | 2026-07-07 |
| [ADR-013](./ADR-013-testing-strategy.md) | Testing strategy — a deterministic test pyramid | Accepted | 2026-07-07 |
| [ADR-014](./ADR-014-scaffolding-toolchain-reality.md) | Sprint 0 scaffolding — toolchain reality vs. canon | Accepted | 2026-07-10 |
| [ADR-015](./ADR-015-oss-dependency-pinning.md) | OSS dependency pinning — MediatR 12.x and FluentAssertions 7.x | Accepted | 2026-07-10 |

## ADR process

1. **Propose.** When a decision is architecturally significant — it is costly to reverse,
   affects multiple components, or constrains future work — open a new ADR using the next
   sequential number (`ADR-NNN-short-slug.md`).
2. **Structure.** Every ADR uses the same sections: **Status**, **Date**, **Context**,
   **Decision**, **Alternatives** (each with an explicit reason for rejection),
   **Consequences** (both positive and negative), and **Related documents**.
3. **Status lifecycle.** An ADR is `Proposed`, then `Accepted` once agreed. A later ADR may
   `Supersede` an earlier one; the superseded ADR is marked `Superseded by ADR-NNN` and kept
   for the historical record — ADRs are append-only and never deleted.
4. **Single source of truth.** ADRs must not contradict the [Documentation Canon](../../CLAUDE.md)
   or the [Architecture](../02-architecture/architecture.md) reference. If a decision changes
   the canon, update the canon in the same change set.
5. **Cross-reference.** Link the ADR from the affected documents (e.g. the architecture ADR
   index) and list its most relevant peers under **Related documents**.

## Related documents

- [Architecture](../02-architecture/architecture.md)
- [Event Model](../02-architecture/event-model.md)
- [Diagrams Index](../diagrams/README.md)
- [Product Vision](../01-product/vision.md)
