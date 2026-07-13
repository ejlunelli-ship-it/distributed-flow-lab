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

**Sprint 3 — Canvas editor: complete.** The React Flow canvas is a controlled view over a
Zustand `canvasStore`: learners drag (or click) `NodeType`s from a data-driven palette, wire
them with directed edges validated against a curated connection matrix — illegal links (e.g.
`Consumer→Producer`) are refused inline with an educational reason (ADR-016) — and edit each
node's type-specific config in a data-driven inspector with client-side validation. Ships the
design-token layer (semantic + per-`NodeType` accents, light/dark) and the Design-mode app
shell with routing. Verified: a Playwright e2e composes `Producer→Exchange→Queue→Consumer` and
proves inline rejection; 41 Vitest unit/component tests, typecheck, ESLint, Prettier and the
production build are green. Sprint 2 delivered SignalR realtime streaming into the SPA store;
Sprint 1 the domain model and deterministic tick-loop engine; Sprint 0 the scaffolding.
The execution plan and per-sprint scope live in
[`docs/05-dev/execution-roadmap.md`](docs/05-dev/execution-roadmap.md); product phases are in
[`docs/01-product/roadmap.md`](docs/01-product/roadmap.md).

## Contributing

Coding standards, testing strategy, and delivery are documented under
[`docs/05-dev/`](docs/05-dev/). Commits follow Conventional Commits. Every change keeps the
documentation synchronized (see [`CLAUDE.md`](CLAUDE.md)).
