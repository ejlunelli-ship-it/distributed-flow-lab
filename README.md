# Distributed Flow Lab

**Distributed Flow Lab (DFL)** is an educational SaaS platform for learning distributed
systems through interactive visual simulations. Learners compose architectures — APIs,
queues, brokers, databases, caches, and distributed services — on a canvas, then run
simulations in which **every animation is driven by a real backend event**. The frontend
never invents state; it renders truth produced by the simulation engine.

> Documentation is the source of truth. Start with [`docs/README.md`](docs/README.md).

## Architecture at a glance

- **Backend** — ASP.NET 8, Clean Architecture (`Api → Infrastructure → Application → Domain`),
  CQRS via MediatR, a `BackgroundService` simulation engine, EF Core/PostgreSQL, and SignalR.
- **Frontend** — React 18 + Vite + TypeScript, React Flow (canvas), Zustand, `@microsoft/signalr`,
  Tailwind, Framer Motion.
- **Realtime contract** — the canonical `SimulationEvent` envelope with a monotonic `sequence`
  streamed over `/hubs/simulation`.

See [`docs/02-architecture/architecture.md`](docs/02-architecture/architecture.md) and the
[ADR log](docs/adr/README.md).

## Repository layout

```
src/     Backend (Clean Architecture layers)
tests/   Backend test projects (unit + integration)
web/     Frontend SPA (React + Vite)
docker/  Dockerfiles + Docker Compose stack
docs/    The documentation set (source of truth)
```

Full rationale: [`docs/05-dev/folder-structure.md`](docs/05-dev/folder-structure.md).

## Prerequisites

- .NET SDK 8+ (the `net8.0` targeting pack; pinned via `global.json`)
- Node.js 22+
- Docker Desktop (for the full stack)

## Getting started

### Backend

```bash
dotnet build DistributedFlowLab.sln          # build all layers (warnings-as-errors)
dotnet test  DistributedFlowLab.sln           # run unit tests
dotnet run --project src/DistributedFlowLab.Api   # serves http://localhost:5080, GET /health
```

### Frontend

```bash
cd web
npm install
npm run dev        # Vite dev server on http://localhost:5173 (proxies /api and /hubs)
npm run test       # Vitest unit/component tests
npm run lint       # ESLint
npm run build      # type-check + production build
npm run e2e        # Playwright end-to-end
```

### Full stack (Docker Compose)

```bash
cp docker/.env.example docker/.env
docker compose -f docker/docker-compose.yml up --build
```

Brings up the MVP container set — `web`, `api`, `rabbitmq`, `postgres`. The SPA is served at
`http://localhost:8080` and reverse-proxies `/api` and `/hubs` to the API. (Kafka and Redis
join the stack in Version 1.)

## Development status

**Sprint 2 — Realtime streaming: complete.** The `SimulationHub` (SignalR) pushes the
authoritative event stream to per-simulation groups (`ReceiveSimulationEvent`,
`SimulationStateChanged`); the SPA subscribes through a reconnecting realtime client and
folds events into a Zustand store with strict `sequence` ordering and gap detection.
Live-verified: gap-free delivery with P95 emit→client latency of 17 ms (target ≤ 250 ms).
Sprint 1 delivered the domain model, the deterministic tick-loop engine and the `/api/v1`
lifecycle endpoints; Sprint 0 the scaffolding. 51 tests across backend and frontend.
The execution plan and per-sprint scope live in
[`docs/05-dev/execution-roadmap.md`](docs/05-dev/execution-roadmap.md); product phases are in
[`docs/01-product/roadmap.md`](docs/01-product/roadmap.md).

## Contributing

Coding standards, testing strategy, and delivery are documented under
[`docs/05-dev/`](docs/05-dev/). Commits follow Conventional Commits. Every change keeps the
documentation synchronized (see [`CLAUDE.md`](CLAUDE.md)).
