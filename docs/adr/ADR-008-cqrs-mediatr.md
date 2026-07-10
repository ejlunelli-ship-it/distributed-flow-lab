# ADR-008: CQRS via MediatR in the Application layer

- **Status:** Accepted
- **Date:** 2026-07-07

## Context

The `DistributedFlowLab.Application` layer holds the system's use cases and the ports that
Infrastructure implements (canon §3, [ADR-004](./ADR-004-clean-architecture.md)). The golden
rules demand that **business logic never lives in controllers** (canon §1; CLAUDE.md): the
Minimal API endpoints (canon §9) and the `SimulationHub` (canon §8) must be thin adapters
that translate transport into application operations and return DTOs.

Those operations divide cleanly into two very different kinds of work:

- **Commands that change state and drive the engine** — create a `Scenario`, create a
  `Simulation`, `start`/`pause`/`resume`/`stop` it, inject a `Fault` (canon §9). These have
  side effects, invariants to enforce (`FluentValidation`, canon §2), and interact with the
  background tick loop ([ADR-007](./ADR-007-background-service-engine.md)).
- **Queries that only read** — list the `Catalog`, fetch a `Scenario` or `Simulation` and
  its state, replay `SimulationEvent`s via `?fromSequence=`, read `MetricSnapshot`s
  (canon §9, §10). These have no side effects and often shape data differently from the
  write model (e.g. aggregated metrics, event pages).

The canon already fixes the tool: **MediatR for CQRS (commands/queries) inside the
Application layer** (canon §2). This ADR records *why* CQRS-via-MediatR is adopted as the
organizing pattern for use cases, and how far the separation goes.

## Decision

We structure the Application layer around **CQRS** — separating **commands** (state-changing
use cases) from **queries** (read-only use cases) — dispatched through **MediatR**.

- **One request type per use case.** Each use case is a MediatR `IRequest` with a dedicated
  handler: e.g. `CreateSimulationCommand`, `StartSimulationCommand`, `InjectFaultCommand`,
  `GetSimulationQuery`, `GetSimulationEventsQuery` (paged by `fromSequence`),
  `GetMetricsQuery`, `GetCatalogQuery`. The handler is the single home of that use case's
  logic, satisfying "business logic never in controllers" by construction (canon §1).
- **Thin transport adapters.** Minimal API endpoints (canon §9) and `SimulationHub` methods
  (canon §8) validate/authenticate the transport concern, `Send` the request to MediatR, and
  map the result to a DTO or problem+json response (RFC 7807, canon §9). They contain no
  domain logic.
- **Commands vs queries as first-class categories.** Commands mutate state (persisted via EF
  Core ports; canon §2) and may signal the runtime (e.g. enqueue a start with the
  `BackgroundService`, [ADR-007](./ADR-007-background-service-engine.md)). Queries are
  side-effect-free reads that can be shaped independently of the write entities — for
  example projecting `SimulationEvent`s and `MetricSnapshot`s (canon §10) into read DTOs
  without disturbing the domain model.
- **Cross-cutting concerns as pipeline behaviors.** Validation (`FluentValidation`), logging,
  and tracing (`OpenTelemetry`/`Serilog`, [ADR-012](./ADR-012-observability-opentelemetry.md))
  are implemented once as MediatR pipeline behaviors, so every command/query gets them
  uniformly instead of each handler re-implementing them.
- **Ports, not concretes.** Handlers depend on Application ports (repositories, event sink,
  messaging adapters); Infrastructure supplies implementations (canon §3). MediatR mediates
  *within* the Application layer; it is not a transport and does not cross the layer boundary
  upward.

This is CQRS as a **use-case separation pattern** (two request pipelines over shared domain
entities), **not** event sourcing and **not** two physical databases — see Alternatives.

## Alternatives

### Direct service classes (no mediator)
Inject hand-written service classes (`ISimulationService`, …) into endpoints. **Rejected as
the primary pattern:** services accrete many unrelated methods over time, blurring use-case
boundaries and encouraging endpoints to call several services and orchestrate logic — the
slide back toward logic-near-transport. MediatR gives one focused handler per use case,
uniform cross-cutting behaviors, and a testable request/response seam. (For genuinely
trivial, logic-free reads a thin service is acceptable; we default to handlers for
consistency.)

### No CQRS — one model, mixed read/write methods
A single set of services/repositories serving both reads and writes. **Rejected:** the read
and write shapes genuinely diverge here — event replay pages and aggregated
`MetricSnapshot`s (canon §10) are nothing like the write entities. Forcing both through one
model either bloats the write model with query concerns or returns write entities to clients,
leaking the domain. Separating the pipelines keeps each side honest.

### Full event-sourced write model
Persist state exclusively as a stream of domain events and rebuild aggregates by replay.
**Rejected for the write model:** Event Sourcing is explicitly a **V3** *product feature to
teach* (canon §13, §14), not the persistence strategy for `Scenario`/`Simulation` metadata,
which is naturally relational and served well by EF Core + PostgreSQL
([ADR-011](./ADR-011-postgres-efcore.md)). Note the distinction: the `SimulationEvent`
`Timeline` *is* an append-only event log ([ADR-009](./ADR-009-event-envelope-sequencing.md)),
but that is the engine's output, not the CQRS write model for entities. Adopting full event
sourcing for entities now would be premature and over-engineered (CLAUDE.md).

### Separate query database only (CQRS with read store)
Maintain a dedicated denormalized read database kept in sync from the write side.
**Rejected for now:** the operational cost (a second store, synchronization, eventual
consistency) is unjustified at MVP–V1 scale. CQRS at the *code* level (separate
command/query handlers over one PostgreSQL) captures the design benefit without the infra
tax; a read store can be introduced later behind the existing query handlers if metrics
volume demands it — an additive change.

## Consequences

### Positive
- **Clear use-case boundaries.** One handler per command/query makes the system's
  capabilities explicit and discoverable, and keeps controllers/hub methods thin
  (canon §1).
- **Uniform cross-cutting concerns.** Validation, logging, and tracing live once in MediatR
  pipeline behaviors, eliminating duplication (canon §2,
  [ADR-012](./ADR-012-observability-opentelemetry.md)).
- **High testability.** Handlers are plain classes with injected ports; they are unit-tested
  with xUnit + FluentAssertions and fakes, no web host or database required
  ([ADR-013](./ADR-013-testing-strategy.md)).
- **Independent read shaping.** Queries project to read DTOs (event pages, metrics) without
  contorting the domain entities.

### Negative
- **Indirection.** Following a call means jumping endpoint → MediatR → handler rather than a
  direct method call, which newcomers must learn. Mitigation: strict one-request-per-use-case
  naming and the components doc make the mapping obvious.
- **Boilerplate for trivial operations.** A tiny read still gets a request + handler.
  Mitigation: the consistency and pipeline-behavior payoff outweighs the ceremony; we do not
  add extra abstraction beyond the request/handler pair (avoid over-engineering, CLAUDE.md).
- **Runtime dispatch.** MediatR resolves handlers via DI at runtime rather than compile-time
  wiring. Mitigation: handler registration is scanned at startup and covered by tests, so a
  missing handler fails fast.

## Related documents

- [Architecture](../02-architecture/architecture.md)
- [API Contracts](../02-architecture/api-contracts.md)
- [CQRS Flow](../diagrams/cqrs-flow.md)
- [ADR-004: Clean Architecture](./ADR-004-clean-architecture.md)
- [ADR-007: Background service engine](./ADR-007-background-service-engine.md)
- [ADR-011: PostgreSQL & EF Core](./ADR-011-postgres-efcore.md)
- [ADR Index](./README.md)
