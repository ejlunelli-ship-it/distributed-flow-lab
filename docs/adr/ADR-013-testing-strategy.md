# ADR-013: Testing strategy — a deterministic test pyramid

- **Status:** Accepted
- **Date:** 2026-07-07

## Context

CLAUDE.md requires that every feature include tests whenever practical, that automated tests
be preferred, and that failing tests be fixed before continuing. DFL's correctness bar is
unusually concrete because of the golden rule: the backend engine is the single source of
truth and the frontend renders its events verbatim
([ADR-006](./ADR-006-backend-source-of-truth.md)). That gives us two properties we can test
rigorously:

- The engine is a **deterministic** function of a `Scenario` plus inputs, advancing on a
  logical `Tick` clock ([ADR-007](./ADR-007-background-service-engine.md)) and emitting an
  ordered `SimulationEvent` stream with monotonic `sequence`
  ([ADR-009](./ADR-009-event-envelope-sequencing.md)). Given a fixed seed and tick schedule,
  it must produce the *same* timeline every time.
- The frontend is a **pure fold** of that event stream into presentation state
  ([ADR-006](./ADR-006-backend-source-of-truth.md),
  [ADR-010](./ADR-010-frontend-stack.md)), so it can be tested against recorded event
  fixtures.

Both sides also meet at **contracts**: the REST shapes (canon §9), the SignalR hub methods
(canon §8), and the event envelope (canon §6). Drift there is the most likely and most
costly failure. The canon fixes the tools: **xUnit + FluentAssertions + Testcontainers**
(backend) and **Vitest + React Testing Library + Playwright** (frontend) (canon §2). This ADR
records how they compose into a strategy.

## Decision

We adopt a **test pyramid** tuned to the engine's determinism and the client/server contract.

- **Backend unit tests (broad base) — xUnit + FluentAssertions.** Domain rules and MediatR
  command/query handlers ([ADR-008](./ADR-008-cqrs-mediatr.md)) are tested in isolation
  against Application ports (fakes/in-memory), with **no** database, broker, or web host —
  the payoff of Clean Architecture ([ADR-004](./ADR-004-clean-architecture.md)).
- **Deterministic, seeded, tick-based engine tests.** The engine is exercised with fixed
  seeds and explicit tick schedules, asserting the produced `SimulationEvent` stream — its
  `type`s, ordering by `sequence`, `tick` grouping, and gap-free monotonicity (canon §6, §7;
  [ADR-009](./ADR-009-event-envelope-sequencing.md)). These are golden-timeline tests: a
  known `Scenario` yields a known event sequence, and any divergence fails the build. This is
  the single most important safety net because it guards the source of truth.
- **Contract tests (REST + SignalR).** Tests assert the REST endpoints (canon §9) and hub
  methods (`ReceiveSimulationEvent(s)`, `Subscribe`/`Unsubscribe`,
  `SimulationStateChanged`; canon §8) match the canonical envelope and DTO shapes (canon §6,
  §10), keeping the backend and the frontend `domain/` types
  ([ADR-010](./ADR-010-frontend-stack.md)) from drifting apart.
- **Backend integration tests — Testcontainers.** Persistence and adapter behavior run
  against **real** PostgreSQL ([ADR-011](./ADR-011-postgres-efcore.md)) and real RabbitMQ /
  Kafka / Redis (canon §2) in ephemeral containers, so broker-fidelity behavior and EF Core
  migrations/queries are verified against the genuine engines, not fakes.
- **Frontend unit/component tests — Vitest + React Testing Library.** Zustand reducers and
  components are tested by feeding recorded `SimulationEvent` fixtures and asserting the
  rendered presentation state and the derived `AnimationStarted`/`AnimationFinished` pacing
  (canon §7) — proving the client renders only what it receives
  ([ADR-006](./ADR-006-backend-source-of-truth.md)).
- **End-to-end tests (narrow top) — Playwright.** A few high-value journeys — compose a
  `Scenario`, run a `Simulation`, watch events animate, scrub the timeline — run against the
  Docker Compose stack ([ADR-005](./ADR-005-docker-compose.md)) to catch wiring failures no
  lower tier can.
- **CI gate.** All tiers run in GitHub Actions (canon §2); a red build blocks merge,
  enforcing "fix failing tests before continuing" (CLAUDE.md).

## Alternatives

### Manual testing
Verify features by hand. **Rejected:** unrepeatable, unscalable across the MVP→V3 roadmap
(canon §14), and incapable of guarding a deterministic event timeline where a single
reordered `sequence` is a real bug. It directly contradicts CLAUDE.md's preference for
automated tests.

### Unit tests only
Test units, skip integration and E2E. **Rejected:** unit tests with fakes cannot prove
broker fidelity (real RabbitMQ DLX, Kafka partitions/offsets, Redis eviction; canon §2) or
that REST/SignalR contracts match the frontend's expectations — exactly where cross-boundary
bugs live. Testcontainers and contract tests exist precisely to close that gap.

### Heavy E2E-only
Rely mainly on broad Playwright journeys. **Rejected:** an inverted pyramid is slow, flaky,
and gives poor failure localization — an E2E failure rarely tells you *which* domain rule
broke. Deterministic engine unit tests pinpoint regressions in the source of truth far
faster and more reliably; E2E is reserved for a thin, high-value top layer.

## Consequences

### Positive
- **Confidence in the source of truth.** Seeded golden-timeline tests lock down the engine's
  deterministic event stream, so refactors that alter behavior fail immediately
  ([ADR-006](./ADR-006-backend-source-of-truth.md),
  [ADR-009](./ADR-009-event-envelope-sequencing.md)).
- **Contract drift caught early.** REST/SignalR contract tests keep backend shapes and
  frontend `domain/` types aligned (canon §6, §8, §9).
- **Real-infrastructure fidelity.** Testcontainers verifies against genuine PostgreSQL and
  brokers, so behavior we teach matches behavior we ship (canon §2,
  [ADR-011](./ADR-011-postgres-efcore.md)).
- **Fast, well-localized feedback.** A broad, cheap unit base with a thin E2E top yields
  quick runs and precise failure signals, keeping CI a reliable merge gate.

### Negative
- **Maintenance cost.** Golden-timeline fixtures and contract tests must be updated when
  behavior or contracts *intentionally* change. Mitigation: that update step is a feature —
  it forces intentional contract evolution and doubles as change documentation.
- **Slower integration/E2E tiers.** Testcontainers and Playwright are heavier than unit
  tests. Mitigation: keep them few and high-value (pyramid shape), run them in parallel CI
  stages, and reserve E2E for critical journeys only.
- **Flakiness risk at the top.** E2E and container tests can be timing-sensitive. Mitigation:
  the engine's logical-clock determinism ([ADR-007](./ADR-007-background-service-engine.md))
  and `sequence`-based resync ([ADR-009](./ADR-009-event-envelope-sequencing.md)) let tests
  assert on event order rather than wall-clock timing, sharply reducing flake.

## Related documents

- [Testing](../05-dev/testing.md)
- [Architecture](../02-architecture/architecture.md)
- [Event Model](../02-architecture/event-model.md)
- [ADR-006: Backend source of truth](./ADR-006-backend-source-of-truth.md)
- [ADR-007: Background service engine](./ADR-007-background-service-engine.md)
- [ADR-009: Event envelope & sequencing](./ADR-009-event-envelope-sequencing.md)
- [ADR Index](./README.md)
