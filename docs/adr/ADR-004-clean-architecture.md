# ADR-004: Clean Architecture layering for the backend

- **Status:** Accepted
- **Date:** 2026-07-07

## Context

DFL is built as a real SaaS product with a long-lived roadmap: MVP through V3 and beyond
(canon §14) progressively adds Kafka, Redis, Retry/DLQ, CQRS, Saga, Circuit Breaker, Event
Sourcing, API Gateway, and more. The backend must absorb this growth without rot. The
golden rules (canon §1, CLAUDE.md) demand Clean Architecture, SOLID, DDD where appropriate,
Dependency Injection, an event-driven core, and — critically — that **business logic never
lives in controllers**.

The core of the system is the Simulation Engine, whose job is to produce an authoritative,
ordered, replayable stream of `SimulationEvent`s (canon §6, §7). That domain logic is the
platform's crown jewel and must be:

- **Framework-independent** — the rules of how a `Node` reacts to a `Message` and which
  events it emits must not depend on EF Core, SignalR, or a broker client.
- **Unit-testable in isolation** — testable without spinning up Postgres, RabbitMQ, or a
  web host.
- **Extensible** — new `NodeType`s, concepts, and broker adapters must be addable without
  editing the core (`../02-architecture/architecture.md` §9).

Canon §3 already fixes the solution structure and, decisively, the **dependency rule**:

> Api → Infrastructure → Application → Domain. Domain depends on nothing.

This ADR records *why* that layering is adopted over the alternatives.

## Decision

We adopt **Clean Architecture** for the backend, realized as four projects with a strict
inward-pointing dependency rule (canon §3):

- **`DistributedFlowLab.Domain`** — entities, value objects, domain events, and enums
  (`Scenario`, `Node`, `Edge`, `Simulation`, `SimulationEvent`, `MetricSnapshot`,
  `NodeType`, status). **No dependencies.**
- **`DistributedFlowLab.Application`** — use cases as **MediatR** commands/queries, **ports**
  (interfaces: event sink, scenario repository, messaging-adapter abstraction), and DTOs.
  Depends on **Domain only**.
- **`DistributedFlowLab.Infrastructure`** — concrete adapters implementing the ports: EF Core
  (PostgreSQL), the SignalR event dispatcher, the RabbitMQ/Kafka/Redis adapters, and the
  Simulation Engine implementation (`BackgroundService`). Depends on **Application**.
- **`DistributedFlowLab.Api`** — the ASP.NET 8 host: Minimal API endpoints (canon §9), the
  `SimulationHub` (canon §8), and the composition root wiring DI. Depends on Infrastructure
  and Application (composition only).

The **Dependency Inversion Principle** is the mechanism: the Application layer declares ports;
Infrastructure implements them; the Api composition root binds interface to implementation.
Controllers/endpoints translate HTTP to MediatR requests and return DTOs — they hold **no**
business logic, satisfying the golden rule directly.

## Alternatives

### Layered / N-tier (Presentation → Business → Data Access)
The classic three-tier stack where the business layer depends *downward* on a data-access
layer. **Rejected:** the dependency points the wrong way — business rules end up depending on
EF Core and broker clients, so the Simulation Engine could not be tested or evolved without
the database and brokers. It also tends to leak persistence concerns (entities-as-tables)
into the domain, corrupting the model we most want to protect.

### Vertical slice only
Organize purely by feature slice (each use case owns its request→handler→persistence) with
no shared Domain/Application/Infrastructure layering. **Rejected as the *sole* structure:**
excellent for CRUD-ish apps, but DFL has a substantial **shared domain core** (the engine,
the event model, the node behaviors) reused across many slices. Without a protected Domain
layer, that core would be duplicated or entangled across slices, and the "Domain depends on
nothing" guarantee would be lost. We do, however, organize handlers feature-by-feature
*inside* the Application layer — vertical slices within Clean Architecture, not instead of
it.

### Transaction Script
Procedural handlers that orchestrate everything inline per request. **Rejected:** the
simulation domain is rich and stateful (tick loop, node state machines, ordering,
compensation) — precisely the case where transaction scripts degenerate into large,
untestable, duplicated procedures. It fights DDD and would embed business logic wherever it
is convenient, including near the transport, violating the golden rules.

## Consequences

### Positive
- The Domain and the Simulation Engine's rules are framework-free and unit-testable with
  xUnit + FluentAssertions, no infrastructure required (canon §2).
- New broker adapters, persistence stores, or transports are added in Infrastructure behind
  existing ports with **zero** changes to Domain or Application — the extensibility the
  roadmap demands (`../02-architecture/architecture.md` §9).
- Business logic is structurally kept out of controllers: endpoints only translate HTTP to
  MediatR requests, honoring the golden rule by construction.
- The layering is legible to new engineers and enforceable (e.g. project-reference
  direction, architecture tests) so the dependency rule cannot silently erode.

### Negative
- More projects, interfaces, and mapping (Domain ↔ DTO) than a single-project app — upfront
  ceremony. Mitigation: the roadmap's scope makes the payoff clear, and we avoid
  over-engineering by adding ports only where a real seam exists (CLAUDE.md "avoid
  overengineering").
- Indirection through ports can obscure the concrete call path for newcomers. Mitigation:
  the composition root centralizes wiring, and the container/component docs make the
  bindings explicit.
- Discipline required: it is always possible to "shortcut" a reference and break the rule.
  Mitigation: enforce the dependency direction via project references and (optionally)
  automated architecture tests in CI.

## Related documents

- [Architecture](../02-architecture/architecture.md)
- [Container Diagram](../diagrams/container-diagram.md)
- [Event Model](../02-architecture/event-model.md)
- [CQRS Flow](../diagrams/cqrs-flow.md)
- [ADR Index](./README.md)
