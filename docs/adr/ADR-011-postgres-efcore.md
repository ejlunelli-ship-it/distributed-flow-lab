# ADR-011: PostgreSQL via EF Core for persistence

- **Status:** Accepted
- **Date:** 2026-07-07

## Context

DFL must durably store the artifacts learners create and the timelines their simulations
produce. The core entities are fixed by the canon (canon §10):

- **Scenario** { id, name, description, conceptTag, nodes[], edges[], createdAt, updatedAt }
- **Node** { id, type: `NodeType`, label, position, config } and
  **Edge** { id, sourceNodeId, targetNodeId, label, config }
- **Simulation** { id, scenarioId, status, currentTick, createdAt, startedAt, endedAt }
- **SimulationEvent** — the canonical envelope, persisted with `sequence` + `simulationId`
- **MetricSnapshot** { simulationId, tick, throughput, avgLatencyMs, inFlight, dlqCount,
  retries }

These are **relational**: `Scenario` owns `Node`s/`Edge`s; `Simulation` references
`Scenario`; `SimulationEvent` and `MetricSnapshot` reference `Simulation`. Access patterns
are equally clear — CRUD on scenarios (canon §9), simulation lifecycle reads/writes, and
**ordered event replay** via `GET /api/v1/simulations/{id}/events?fromSequence=` (canon §9),
which needs an index on `(simulationId, sequence)` to page the `Timeline` cheaply
([ADR-009](./ADR-009-event-envelope-sequencing.md)).

Persistence is an Infrastructure concern behind Application ports (repositories), never
touching Domain (canon §3, [ADR-004](./ADR-004-clean-architecture.md)). The store must also
fit the local topology as a `postgres` container in Docker Compose (canon §2,
[ADR-005](./ADR-005-docker-compose.md)). The canon fixes the choice: **EF Core (PostgreSQL)**
(canon §2). This ADR records why.

## Decision

We use **PostgreSQL** as the relational database, accessed through **EF Core** from the
`DistributedFlowLab.Infrastructure` layer.

- **Relational model.** `Scenario`, `Simulation`, `SimulationEvent`, and `MetricSnapshot`
  (canon §10) map to tables with foreign keys expressing ownership/reference. `Node`s and
  `Edge`s belonging to a `Scenario` are persisted as owned/related data; their type-specific
  `config` is stored as PostgreSQL `jsonb`, giving schema flexibility for varied `NodeType`
  configuration without a table per type.
- **EF Core behind ports.** Repository interfaces declared in Application (e.g. scenario and
  simulation repositories, the event sink/reader) are implemented with EF Core `DbContext`s
  in Infrastructure; Domain has no persistence dependency (canon §3). Query handlers
  ([ADR-008](./ADR-008-cqrs-mediatr.md)) read through these ports.
- **Event timeline storage.** `SimulationEvent`s are persisted append-only with their
  monotonic `sequence` and `simulationId`, indexed on `(simulationId, sequence)` so replay
  and gap-fill via `?fromSequence=` are efficient, ordered scans
  ([ADR-009](./ADR-009-event-envelope-sequencing.md)).
- **Migrations.** Schema is versioned with EF Core migrations, applied on deploy, keeping the
  database shape in source control alongside the model.
- **Container.** Runs as the `postgres` service in Docker Compose for local dev
  (canon §2, [ADR-005](./ADR-005-docker-compose.md)); integration tests use Testcontainers
  against real PostgreSQL ([ADR-013](./ADR-013-testing-strategy.md)).

## Alternatives

### SQL Server
A first-class EF Core provider. **Rejected:** PostgreSQL is fully open-source with no
licensing constraints for a SaaS product, has first-rate `jsonb` support for our
type-specific `config`, and containerizes lightly for local dev and CI. EF Core's provider
abstraction means we keep the ORM benefits without the SQL Server licensing and footprint.

### MongoDB (document store)
Store scenarios and events as documents. **Rejected:** the model is fundamentally relational
(`Scenario`↔`Simulation`↔`SimulationEvent`↔`MetricSnapshot`, canon §10) and benefits from
foreign keys and transactional integrity when a simulation and its events must stay
consistent. We already get document flexibility exactly where we need it via PostgreSQL
`jsonb` for node/edge `config`, without giving up relational guarantees for everything else.

### In-memory only
Keep everything in process. **Rejected:** `Scenario`s are reusable blueprints that must
survive restarts (canon §5), and the `Timeline` must be replayable long after a run ends
(canon §5, §9) — including resuming a persisted simulation's `sequence` after a host restart
([ADR-007](./ADR-007-background-service-engine.md)). Volatile storage cannot meet the
product's save/load (roadmap V1) and replay (V2) requirements (canon §14). An in-memory
provider is, however, useful for fast tests behind the same ports.

### Dedicated event store database (e.g. EventStoreDB)
Use a purpose-built append-only event database for the timeline. **Rejected:** it is
warranted for full Event Sourcing of the write model, which we explicitly do **not** adopt
now — Event Sourcing is a **V3 feature to teach** (canon §13, §14),
[ADR-008](./ADR-008-cqrs-mediatr.md). The `SimulationEvent` timeline is an append-only table
with an indexed `(simulationId, sequence)`, which PostgreSQL serves well; adding a second
datastore now is operational overhead and over-engineering (CLAUDE.md).

## Consequences

### Positive
- **Relational integrity.** Foreign keys and transactions keep `Scenario`/`Simulation`/event
  data consistent, matching the data-model canon (canon §10).
- **Efficient replay.** An indexed `(simulationId, sequence)` timeline makes
  `?fromSequence=` paging and reconnect gap-fill cheap
  ([ADR-009](./ADR-009-event-envelope-sequencing.md)).
- **Flexible config, relational core.** `jsonb` absorbs heterogeneous `NodeType`/edge
  `config` without a table-per-type explosion, while everything else stays relational.
- **Migrations in source control.** EF Core migrations version the schema with the model, and
  Testcontainers exercises the real engine in CI
  ([ADR-013](./ADR-013-testing-strategy.md)).

### Negative
- **ORM overhead and leaky abstractions.** EF Core can generate suboptimal queries and its
  change-tracking adds cost on hot paths. Mitigation: read-heavy query handlers can use
  projections/`AsNoTracking`, and the event append path is simple inserts; ports let us drop
  to raw SQL where a real bottleneck is proven.
- **High-volume event growth.** Long or numerous simulations accumulate many
  `SimulationEvent` rows. Mitigation: partition/retention policies on the timeline table and
  batched inserts; the `(simulationId, sequence)` index keeps reads bounded.
- **Migration discipline.** Schema changes require reviewed, ordered migrations. Mitigation:
  migrations are code-reviewed and applied automatically on deploy, and integration tests run
  against the migrated schema.

## Related documents

- [Architecture](../02-architecture/architecture.md)
- [Data Model](../02-architecture/data-model.md)
- [API Contracts](../02-architecture/api-contracts.md)
- [ADR-004: Clean Architecture](./ADR-004-clean-architecture.md)
- [ADR-008: CQRS via MediatR](./ADR-008-cqrs-mediatr.md)
- [ADR-009: Event envelope & sequencing](./ADR-009-event-envelope-sequencing.md)
- [ADR Index](./README.md)
