# ADR-012: Observability with OpenTelemetry and Serilog

- **Status:** Accepted
- **Date:** 2026-07-07

## Context

DFL is built as a real SaaS product (canon §0; CLAUDE.md), and its backend is a non-trivial
distributed system in its own right: a `BackgroundService` tick loop
([ADR-007](./ADR-007-background-service-engine.md)) emits `SimulationEvent`s that fan out to
clients over `SimulationHub` (canon §8) and are persisted to PostgreSQL
([ADR-011](./ADR-011-postgres-efcore.md)), while real broker adapters — RabbitMQ, Kafka,
Redis (canon §2) — participate inside ticks. When a simulation stalls, a client desyncs, or a
broker adapter misbehaves, engineers must be able to answer *what happened and in what order*
across these components.

The canonical event envelope already carries correlation identifiers built for this:
`correlationId` (the `messageId` a message-related event belongs to) and `traceId` (canon
§6). Propagating and honoring those across the request → engine → dispatch → persistence path
turns a pile of logs into a coherent, queryable story. The canon fixes the tools:
**OpenTelemetry (traces/metrics/logs)** and **Serilog structured logging** (canon §2). This
ADR records adopting them and how they use the canonical correlation identifiers.

## Decision

We adopt **OpenTelemetry** as the vendor-neutral instrumentation layer for **traces,
metrics, and logs**, with **Serilog** as the structured logging implementation, across the
`DistributedFlowLab.Api` and `Infrastructure` layers.

- **Traces.** OpenTelemetry spans cover the request lifecycle (Minimal API endpoints,
  canon §9), MediatR command/query handling via a pipeline behavior
  ([ADR-008](./ADR-008-cqrs-mediatr.md)), engine tick processing
  ([ADR-007](./ADR-007-background-service-engine.md)), SignalR dispatch (canon §8), EF Core
  queries ([ADR-011](./ADR-011-postgres-efcore.md)), and calls into the RabbitMQ/Kafka/Redis
  adapters. The envelope's **`traceId`** (canon §6) is aligned with the trace context so an
  event on the wire can be tied back to the span that produced it.
- **Correlation propagation.** `correlationId` and `traceId` are propagated end-to-end and
  attached to spans and log entries, so all activity for one `messageId` or one request can
  be retrieved together across engine, transport, and persistence (canon §6).
- **Metrics.** OpenTelemetry metrics capture operational health (request rates/latencies,
  hub connection counts, tick durations, event emission/dispatch throughput, adapter
  latencies). These are **operational** telemetry and are kept distinct from the **domain**
  `MetricSnapshot` (throughput, avgLatencyMs, inFlight, dlqCount, retries; canon §10) that
  the engine computes *about the simulation* and serves via
  `GET /api/v1/simulations/{id}/metrics` (canon §9) — the latter is product data, the former
  is system telemetry.
- **Structured logs via Serilog.** All logging is structured (key/value properties, not
  string concatenation), enriched with `traceId`/`correlationId`/`simulationId`, and emitted
  through the OpenTelemetry logging pipeline so logs, traces, and metrics share correlation.
- **Vendor-neutral export.** Instrumentation targets the OpenTelemetry Protocol (OTLP) so any
  compatible backend can be plugged in without touching application code; a collector can run
  alongside the Docker Compose stack (canon §2,
  [ADR-005](./ADR-005-docker-compose.md)) for local development.

## Alternatives

### Ad-hoc logging (unstructured `ILogger` strings only)
Rely on plain log statements with no tracing or metrics. **Rejected:** unstructured logs
cannot be correlated across the request → tick → dispatch → persistence path, so diagnosing
an ordering or desync problem — inherently a *distributed* problem — becomes guesswork. It
also wastes the `correlationId`/`traceId` the envelope already carries (canon §6) and falls
short of the production-quality bar (CLAUDE.md).

### Vendor-specific APM only (single proprietary agent)
Adopt one commercial APM SDK throughout. **Rejected:** it couples the codebase to a vendor's
API and pricing and complicates local/CI development (canon §2,
[ADR-005](./ADR-005-docker-compose.md)). OpenTelemetry gives the same three signals through a
neutral standard; a vendor backend can still consume OTLP later without code changes. We
prefer the portable seam.

### No tracing (metrics + logs only)
Instrument metrics and logs but skip distributed traces. **Rejected:** traces are exactly
what reconstructs *ordering and causality* across components — the most valuable signal for a
system whose correctness is defined by event order
([ADR-009](./ADR-009-event-envelope-sequencing.md)). Dropping traces would discard the
`traceId` linkage the envelope is designed around.

## Consequences

### Positive
- **Portable, standard observability.** OpenTelemetry + OTLP avoids vendor lock-in; any
  compatible backend works, and instrumentation is written once (canon §2).
- **End-to-end correlation.** `traceId`/`correlationId` propagation (canon §6) ties logs,
  traces, and metrics together across engine, transport, and persistence — fast diagnosis of
  desyncs and stalls.
- **Clear separation of concerns.** Operational telemetry (OpenTelemetry metrics) stays
  distinct from domain `MetricSnapshot` product data (canon §10), so neither pollutes the
  other.
- **Structured logs by default.** Serilog's structured events are queryable and enriched,
  matching production-quality expectations (CLAUDE.md).

### Negative
- **Setup and configuration cost.** Instrumentation, a collector, and exporter wiring add
  moving parts. Mitigation: OpenTelemetry's ASP.NET/EF Core/HTTP auto-instrumentation covers
  most spans with minimal code, and the collector is a single Compose service locally
  ([ADR-005](./ADR-005-docker-compose.md)).
- **Telemetry overhead.** Tracing/metrics add per-operation cost and data volume. Mitigation:
  sampling for traces and sensible metric cardinality keep overhead bounded; the hot event
  path is measured rather than assumed.
- **Another dependency surface.** OpenTelemetry libraries evolve and must be kept current.
  Mitigation: dependencies are pinned and updated deliberately, and the OTLP boundary
  isolates us from backend-specific churn.

## Related documents

- [Architecture](../02-architecture/architecture.md)
- [Observability](../06-learning/observability.md)
- [Event Model](../02-architecture/event-model.md)
- [ADR-007: Background service engine](./ADR-007-background-service-engine.md)
- [ADR-009: Event envelope & sequencing](./ADR-009-event-envelope-sequencing.md)
- [ADR-005: Docker Compose](./ADR-005-docker-compose.md)
- [ADR Index](./README.md)
