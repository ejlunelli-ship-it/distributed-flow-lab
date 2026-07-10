# ADR-005: Docker Compose for local development and orchestration

- **Status:** Accepted
- **Date:** 2026-07-07

## Context

A DFL developer needs to run the full system to work on any non-trivial feature, because
the product is end-to-end by nature: a user action hits the REST API, the Simulation Engine
drives a **real broker** (ADR-003), events are persisted to PostgreSQL, and the stream is
pushed to the browser over SignalR (ADR-002). The canonical container set is fixed by canon
§2: `web`, `api`, `rabbitmq`, `kafka` (+`zookeeper` or KRaft), `redis`, `postgres`.

Requirements for the local/dev orchestration:

- **One-command bring-up** of the whole topology so any engineer (or CI job) reaches a
  working system quickly and reproducibly.
- **Realistic wiring** — the `web` container reverse-proxies `/api/v1` and
  `/hubs/simulation` to `api`; `api` reaches `postgres`, `rabbitmq`, `kafka`, `redis` by
  service name on a shared network (`../02-architecture/architecture.md` §6).
- **Persistence across restarts** for Postgres (and broker data where useful) via named
  volumes, without polluting the host.
- **Low barrier to entry** — no manual installation of five different servers at five
  different versions on every developer's machine.
- **Parity with CI**, where Testcontainers already runs the same brokers for integration
  tests (canon §2).

We are choosing the mechanism for orchestrating these containers for local development and
demos, explicitly *not* the production cloud deployment model.

## Decision

We use **Docker + Docker Compose** as the local development and orchestration mechanism,
with a single `docker-compose.yml` defining the canonical services `web`, `api`,
`rabbitmq`, `kafka` (KRaft mode preferred, `zookeeper` otherwise), `redis`, and `postgres`
(canon §2).

The compose file specifies, for each service: image (or build context for `web`/`api`),
port mappings, environment/config, named volumes for stateful services (`postgres`,
optionally `rabbitmq`/`kafka`), a shared bridge network for service-name DNS, health checks,
and `depends_on` ordering so `api` starts after its datastores are healthy. `web`
reverse-proxies `/api/v1` and `/hubs/simulation` to `api`. The deployment topology is
documented in `../diagrams/deployment-diagram.md`.

Compose is scoped to **local development, demos, and CI-adjacent workflows**. Production
cloud deployment (multi-tenant SaaS, canon §14 Future) is a separate concern that can adopt
an orchestrator later without changing the container images this decision produces.

## Alternatives

### Local installs (native servers on the host)
Install PostgreSQL, RabbitMQ, Kafka, and Redis directly on each developer machine.
**Rejected:** version drift and "works on my machine" divergence, cross-platform install
pain (Windows/macOS/Linux), no clean teardown, and painful parallel-version management.
It directly undermines the reproducibility that makes a five-service system approachable.

### Kubernetes / minikube locally
Run the stack on a local Kubernetes cluster. **Rejected for local dev:** heavyweight and
high-friction for the inner development loop — manifests/Helm charts, a local control plane,
image loading, and slower iteration, all for a single-developer environment. The value of
Kubernetes (scheduling, self-healing, scaling across nodes) is a **production/ops** concern,
not a local-dev one; adopting it here is over-engineering (CLAUDE.md). Because we ship plain
container images, a future production deployment can still choose Kubernetes without
reworking this decision.

### Dev containers only
Rely solely on a VS Code Dev Container / `devcontainer.json`. **Rejected as the primary
mechanism:** dev containers configure a single *development environment* container, not the
multi-service application topology; they typically delegate to Docker Compose for the
surrounding services anyway. It also couples the workflow to a specific editor. A dev
container may *wrap* our compose file for convenience, but Compose remains the source of
truth for the topology.

## Consequences

### Positive
- `docker compose up` yields the entire canonical topology reproducibly on any developer
  machine and in CI, matching the documented deployment diagram.
- Service-name networking mirrors the real wiring (`web`→`api`→brokers/db), so integration
  behavior surfaces locally rather than only in a shared environment.
- Named volumes persist Postgres data across restarts without host pollution and are trivial
  to reset (`docker compose down -v`).
- The same container images are the unit of deployment later, giving a clean path to a
  cloud orchestrator without image changes.

### Negative
- Running five-plus services locally consumes meaningful RAM/CPU; Kafka in particular is
  resource-hungry. Mitigation: KRaft mode (no ZooKeeper) trims footprint; developers can
  bring up a subset (e.g. omit Kafka for RabbitMQ-only work) via compose profiles.
- Compose is not a production orchestrator (no autoscaling, self-healing, rolling deploys).
  Mitigation: this is by design and scope-limited; production orchestration is a separate,
  later decision on the same images.
- First-run image pulls/builds are slow on cold caches. Mitigation: pinned image tags,
  layer caching, and CI image caching.

## Related documents

- [Architecture](../02-architecture/architecture.md)
- [Deployment Diagram](../diagrams/deployment-diagram.md)
- [Container Diagram](../diagrams/container-diagram.md)
- [ADR-003: Real RabbitMQ adapter](./ADR-003-rabbitmq.md)
- [ADR Index](./README.md)
