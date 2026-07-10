# Local Development

This guide gets **Distributed Flow Lab (DFL)** running on a local machine end-to-end: the
**ASP.NET 8** API, the **React 18 + Vite** frontend, and the backing infrastructure
(`rabbitmq`, `kafka`, `redis`, `postgres`) via **Docker Compose**. Follow it top to bottom for a
first-time setup.

The intended developer loop is: **run infrastructure in containers, run `api` and `web` on the
host with hot reload.** This gives realistic broker/DB behavior with the fastest inner loop.

---

## 1. Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| **.NET 8 SDK** | 8.0.x | Build/run the backend solution and EF Core migrations |
| **Node.js** | LTS (20.x+) | Build/run the `web/` frontend |
| **Docker Desktop** | latest | Run `rabbitmq`, `kafka`, `redis`, `postgres` (and full-stack compose) |
| **Git** | latest | Clone the repository |

Verify:

```bash
dotnet --version     # 8.0.x
node --version       # v20+ (LTS)
docker --version     # Docker Desktop running
```

Recommended: an editor with C# and ESLint/Prettier support (e.g. VS Code or Rider) so
`.editorconfig`, ESLint, and Prettier apply automatically.

---

## 2. Clone

```bash
git clone <repository-url> distributed-flow-lab
cd distributed-flow-lab
```

Repository layout is documented in [Folder Structure](./folder-structure.md).

---

## 3. Start infrastructure with Docker Compose

Bring up only the backing services (not `api`/`web`) so you can run those on the host:

```bash
cd docker
cp .env.example .env          # review/adjust local values first
docker compose up -d rabbitmq kafka redis postgres
docker compose ps             # confirm all are "healthy"
```

Wait until healthchecks report **healthy** before starting the API (Kafka in particular takes a
few seconds to become ready). Compose service definitions, healthchecks, and volumes are
documented in [Docker](./docker.md).

### Default ports

| Service | Host port | Notes |
|---------|-----------|-------|
| `postgres` | `5432` | Metadata + persisted `SimulationEvent`s |
| `redis` | `6379` | Cache + pub/sub |
| `rabbitmq` | `5672` (AMQP), `15672` (management UI) | Management UI at http://localhost:15672 |
| `kafka` | `9092` | Broker (KRaft mode) |
| `api` | `8080` | ASP.NET host (REST `/api/v1`, hub `/hubs/simulation`) |
| `web` | `5173` | Vite dev server |

---

## 4. Configure environment

Backend configuration follows the standard ASP.NET precedence: `appsettings.json` <
`appsettings.Development.json` < environment variables < user-secrets. **No secrets are committed**
— provide them via environment variables or user-secrets.

Key settings (names shown as environment-variable form):

```bash
# Connection strings
ConnectionStrings__Postgres="Host=localhost;Port=5432;Database=dfl;Username=dfl;Password=dfl"
ConnectionStrings__Redis="localhost:6379"

# Messaging
RabbitMq__Host="localhost"
RabbitMq__Port="5672"
RabbitMq__Username="guest"
RabbitMq__Password="guest"
Kafka__BootstrapServers="localhost:9092"

# Realtime / CORS (allow the Vite dev origin to reach the hub + API)
Cors__AllowedOrigins__0="http://localhost:5173"

# Observability
Serilog__MinimumLevel="Information"
OTEL_EXPORTER_OTLP_ENDPOINT="http://localhost:4317"   # optional local collector
```

For local secrets prefer user-secrets over exporting in your shell:

```bash
cd src/DistributedFlowLab.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Port=5432;Database=dfl;Username=dfl;Password=dfl"
```

Frontend configuration uses Vite env files (`web/.env.local`):

```bash
VITE_API_BASE_URL="http://localhost:8080/api/v1"
VITE_SIGNALR_HUB_URL="http://localhost:8080/hubs/simulation"
```

---

## 5. Apply database migrations

EF Core migrations create the schema in PostgreSQL:

```bash
# one-time: install the EF tooling
dotnet tool install --global dotnet-ef

# from the repository root
dotnet ef database update \
  --project src/DistributedFlowLab.Infrastructure \
  --startup-project src/DistributedFlowLab.Api
```

Migration authoring and CI application are covered in [Deployment](./deployment.md).

---

## 6. Seed the catalog

The **Catalog** is the library of concept-focused `Scenario`s (RabbitMQ, Kafka, Saga, CQRS, …).
Seed it so the frontend has content to browse on first run:

```bash
# runs the idempotent catalog seeder against the local database
dotnet run --project src/DistributedFlowLab.Api -- seed-catalog
```

The seeder is idempotent — re-running it will not duplicate scenarios. After seeding, verify:

```bash
curl http://localhost:8080/api/v1/catalog
```

---

## 7. Run the API

```bash
dotnet run --project src/DistributedFlowLab.Api
# or, for hot reload:
dotnet watch --project src/DistributedFlowLab.Api run
```

The API listens on **http://localhost:8080**. Sanity checks:

```bash
curl http://localhost:8080/health          # liveness/readiness
curl http://localhost:8080/api/v1/catalog   # catalog scenarios
```

The `SimulationHub` is available at `http://localhost:8080/hubs/simulation`.

---

## 8. Run the frontend

```bash
cd web
npm install
npm run dev
```

Open **http://localhost:5173**. The app connects to the API and, once a `Simulation` runs,
subscribes to its `simulationId` group over SignalR and renders incoming `SimulationEvent`s.

---

## 9. Full local flow (verification)

1. Open the app; browse the **Catalog** and open a RabbitMQ scenario onto the **canvas**.
2. Create and start a **Simulation** (`POST /api/v1/simulations` then `.../start`).
3. Watch message tokens animate along edges — each animation is driven by backend events
   (`MessagePublished` → `MessageRouted` → `MessageEnqueued` → `MessageDequeued` →
   `MessageProcessed` → `AckReceived`).
4. Open the **inspector** to view the raw `Timeline`.
5. Inject a fault (`POST /api/v1/simulations/{id}/faults`) and observe `FaultInjected` /
   `DeadLettered` behavior.

If the animations play and the inspector shows a gap-free `sequence`, the environment is correct.

---

## 10. Alternative: run everything in containers

To run the entire stack (including `api` and `web`) in Docker instead of on the host:

```bash
cd docker
docker compose up --build
```

The dev override applies source mounts and hot reload. Use this to reproduce CI-like conditions
or when you are not actively editing a given service. See [Docker](./docker.md).

---

## 11. Troubleshooting

| Symptom | Likely cause | Resolution |
|---------|-------------|------------|
| API fails on startup with a DB connection error | Postgres not healthy yet, or wrong connection string | `docker compose ps`; confirm `ConnectionStrings__Postgres` matches `.env` credentials |
| `dotnet ef` cannot find the DbContext | Wrong `--project`/`--startup-project` | Use the exact commands in §5 from the repo root |
| Frontend loads but no events animate | Hub URL/CORS mismatch | Confirm `VITE_SIGNALR_HUB_URL` and `Cors__AllowedOrigins` include `http://localhost:5173` |
| SignalR connects then drops repeatedly | API restarting / negotiation blocked | Check API logs; ensure WebSockets aren't blocked by a proxy |
| Kafka adapter errors on startup | Kafka not ready | Wait for the `kafka` healthcheck; then restart the API |
| Empty catalog in the UI | Seeder not run | Run §6 `seed-catalog`; verify with the `/api/v1/catalog` curl |
| Port already in use (5432/6379/5672/9092/8080/5173) | Local process holding the port | Stop the conflicting process or remap the port in `docker/.env` |
| `docker compose up` stale after config change | Cached containers | `docker compose down -v && docker compose up --build` |

For a deeper reset, `docker compose down -v` removes volumes (this wipes local DB/broker state —
re-run migrations (§5) and the seeder (§6) afterward).

---

## Related documents

- [Docker](./docker.md)
- [Technologies](./technologies.md)
- [Folder Structure](./folder-structure.md)
- [Coding Standards](./coding-standards.md)
- [Testing](./testing.md)
- [Deployment](./deployment.md)
- [Architecture](../02-architecture/architecture.md)
