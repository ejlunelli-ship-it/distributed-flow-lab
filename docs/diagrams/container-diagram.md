# Container Diagram (C4 Level 2)

This diagram opens the **Distributed Flow Lab** box from the
[System Context](./system-context.md) and shows the deployable/executable containers inside
it and how responsibility is divided among them. It follows the Clean Architecture layering
(canon §3, [ADR-004](../adr/ADR-004-clean-architecture.md)): the API host owns the transport
and hosts the Simulation Engine, which drives the real broker adapters and persists the
event timeline.

```mermaid
C4Container
    title DFL — Container Diagram

    Person(user, "Learner", "Any of the five personas (canon §11)")

    System_Boundary(dfl, "Distributed Flow Lab") {
        Container(web, "Web SPA", "React 18 + Vite + React Flow + Zustand", "Canvas editor, simulation playback, event inspector. Renders animations from backend events only.")
        Container(api, "API Host", "ASP.NET 8 Minimal APIs", "REST endpoints /api/v1; composition root; DI wiring. Business logic delegated to Application (MediatR).")
        Container(hub, "SignalR Hub", "SignalR — SimulationHub @ /hubs/simulation", "Pushes SimulationEvents to clients grouped by simulationId.")
        Container(engine, "Simulation Engine", "BackgroundService tick loop (Infrastructure)", "Advances ticks, executes scenario topology, emits canonical SimulationEvents.")
        Container(adapters, "Messaging Adapters", "RabbitMQ / Kafka / Redis adapters (Infrastructure)", "Implement the messaging port; drive real broker behavior behind the engine.")
        ContainerDb(pg, "PostgreSQL", "EF Core", "Scenarios, Simulations, SimulationEvents, MetricSnapshots.")
    }

    System_Ext(rabbitmq, "RabbitMQ", "AMQP")
    System_Ext(kafka, "Apache Kafka", "Topics/partitions")
    System_Ext(redis, "Redis", "Cache + pub/sub + SignalR backplane")

    Rel(user, web, "Uses", "HTTPS / WSS")
    Rel(web, api, "REST calls", "HTTPS /api/v1")
    Rel(web, hub, "Realtime events", "WSS /hubs/simulation")
    Rel(api, engine, "Create/start/pause/resume/stop/inject fault", "MediatR")
    Rel(engine, adapters, "Publish / consume / ack via messaging port")
    Rel(engine, hub, "Dispatch SimulationEvents", "in-process")
    Rel(engine, pg, "Persist events & metrics", "EF Core")
    Rel(hub, redis, "Backplane fan-out (multi-instance)", "Redis pub/sub")
    Rel(adapters, rabbitmq, "AMQP", "exchanges/queues/DLX")
    Rel(adapters, kafka, "Kafka protocol", "topics/partitions/offsets")
    Rel(adapters, redis, "RESP", "cache hit/miss/evict")
```

## Legend & explanation

- **Web SPA** (`web/`, canon §4) — React 18 + Vite. The [React Flow](../adr/ADR-001-react-flow.md)
  canvas, the Zustand stores, the SignalR client (`realtime/`), and the inspector live here.
  It **renders backend events and invents no state** (canon §1).
- **API Host** (`DistributedFlowLab.Api`, canon §3) — ASP.NET 8 Minimal API endpoints
  (canon §9) and the composition root. Endpoints translate HTTP into MediatR requests; no
  business logic sits in controllers ([ADR-004](../adr/ADR-004-clean-architecture.md)).
- **SignalR Hub** — `SimulationHub` at `/hubs/simulation` (canon §8). Server→client
  `ReceiveSimulationEvent` / `ReceiveSimulationEvents` / `SimulationStateChanged`;
  client→server `Subscribe` / `Unsubscribe`; one group per `simulationId`
  ([ADR-002](../adr/ADR-002-signalr.md)).
- **Simulation Engine** — the `BackgroundService` tick loop in Infrastructure. It is the
  single source of truth: it advances `tick`s, runs node behaviors, and emits canonical
  `SimulationEvent`s (canon §6, §7) with a monotonic `sequence`.
- **Messaging Adapters** — RabbitMQ/Kafka/Redis adapters implementing the Application
  messaging **port**. Real brokers give production fidelity ([ADR-003](../adr/ADR-003-rabbitmq.md)).
- **PostgreSQL** — persists scenarios, simulations, the event timeline, and metric
  snapshots (canon §10), enabling replay via `GET /api/v1/simulations/{id}/events`.

The engine→hub→web chain is the load-bearing path of the whole product: it is what makes
every animation a rendering of a real backend event. It is detailed step-by-step in
[Message Flow](./message-flow.md).

## Related documents

- [System Context](./system-context.md)
- [Deployment Diagram](./deployment-diagram.md)
- [Message Flow](./message-flow.md)
- [Architecture](../02-architecture/architecture.md)
- [ADR-004: Clean Architecture](../adr/ADR-004-clean-architecture.md)
- [Diagrams Index](./README.md)
