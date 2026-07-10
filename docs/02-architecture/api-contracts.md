# API Contracts

> REST contract reference for Distributed Flow Lab. Contracts only — no implementation.
> Base path **`/api/v1`**, JSON, camelCase. All errors use RFC 7807 problem+json. Endpoints
> and shapes follow the [canon §9–§10](../../CLAUDE.md).

## 1. Resource overview

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/api/v1/catalog` | List catalog concept scenarios. |
| GET | `/api/v1/scenarios` | List the caller's scenarios. |
| POST | `/api/v1/scenarios` | Create a scenario. |
| GET | `/api/v1/scenarios/{id}` | Get a scenario. |
| PUT | `/api/v1/scenarios/{id}` | Replace/update a scenario. |
| DELETE | `/api/v1/scenarios/{id}` | Delete a scenario. |
| POST | `/api/v1/simulations` | Create a simulation from a scenario. |
| GET | `/api/v1/simulations/{id}` | Get a simulation + current state. |
| POST | `/api/v1/simulations/{id}/start` | Start the simulation. |
| POST | `/api/v1/simulations/{id}/pause` | Pause the simulation. |
| POST | `/api/v1/simulations/{id}/resume` | Resume the simulation. |
| POST | `/api/v1/simulations/{id}/stop` | Stop the simulation. |
| POST | `/api/v1/simulations/{id}/faults` | Inject a fault. |
| GET | `/api/v1/simulations/{id}/events?fromSequence=` | Replay/history of events. |
| GET | `/api/v1/simulations/{id}/metrics` | Current metrics snapshot(s). |

Conventions: `application/json` request/response; authenticated via bearer token;
timestamps ISO-8601 UTC; identifiers are GUID strings.

## 2. Catalog

### GET /api/v1/catalog
Lists published concept scenarios (RabbitMQ, Kafka, Saga, CQRS, …).

Response `200`:
```json
[
  { "id": "cat-rabbitmq-basics", "name": "RabbitMQ Basics", "description": "Exchange → queue → consumer with ack.", "conceptTag": "RabbitMQ" },
  { "id": "cat-kafka-partitions", "name": "Kafka Partitions", "description": "Keyed partitioning and consumer groups.", "conceptTag": "Kafka" }
]
```

## 3. Scenarios

### GET /api/v1/scenarios
Response `200`: array of scenario summaries (`id`, `name`, `description`, `conceptTag`,
`updatedAt`).

### POST /api/v1/scenarios
Request:
```json
{
  "name": "Order pipeline",
  "description": "Producer to queue to consumer",
  "conceptTag": "RabbitMQ",
  "nodes": [
    { "id": "node-producer-1", "type": "Producer", "label": "Order API", "position": { "x": 40, "y": 120 }, "config": {} },
    { "id": "node-exchange-1", "type": "Exchange", "label": "orders.ex", "position": { "x": 260, "y": 120 }, "config": { "exchangeType": "direct" } },
    { "id": "node-queue-1", "type": "Queue", "label": "orders.q", "position": { "x": 480, "y": 120 }, "config": { "dlx": "node-dlq-1" } }
  ],
  "edges": [
    { "id": "edge-1", "sourceNodeId": "node-producer-1", "targetNodeId": "node-exchange-1", "label": "publish", "config": {} },
    { "id": "edge-2", "sourceNodeId": "node-exchange-1", "targetNodeId": "node-queue-1", "label": "order.created", "config": { "routingKey": "order.created" } }
  ]
}
```
Response `201` (Location: `/api/v1/scenarios/{id}`): the created scenario including `id`,
`createdAt`, `updatedAt`.
Status codes: `201`, `400` (validation), `401`, `409` (name conflict).

### GET /api/v1/scenarios/{id}
Response `200`: full scenario (`id`, `name`, `description`, `conceptTag`, `nodes[]`,
`edges[]`, `createdAt`, `updatedAt`). `404` if not found; `403` if not owner.

### PUT /api/v1/scenarios/{id}
Request: same shape as POST body (full replace). Response `200` updated scenario.
Status: `200`, `400`, `403`, `404`, `409`.

### DELETE /api/v1/scenarios/{id}
Response `204`. Status: `204`, `403`, `404`, `409` (referenced by a running simulation).

## 4. Simulations

### POST /api/v1/simulations
Create a simulation instance from a scenario.
Request:
```json
{ "scenarioId": "a12...-guid", "options": { "maxTicks": 500, "tickIntervalMs": 200 } }
```
Response `201` (Location: `/api/v1/simulations/{id}`):
```json
{
  "id": "sim-90a...-guid",
  "scenarioId": "a12...-guid",
  "status": "Draft",
  "currentTick": 0,
  "createdAt": "2026-07-07T15:30:00Z",
  "startedAt": null,
  "endedAt": null
}
```
Status: `201`, `400`, `403`, `404` (unknown scenario).

### GET /api/v1/simulations/{id}
Response `200`: the simulation with `status` (`Draft|Running|Paused|Completed|Stopped|Failed`)
and `currentTick`. `404`/`403` as usual.

### Lifecycle transitions
`POST /api/v1/simulations/{id}/start` · `/pause` · `/resume` · `/stop`

Each returns `200` with the updated simulation state and triggers the corresponding
lifecycle event (`SimulationStarted`, `SimulationPaused`, `SimulationResumed`,
`SimulationStopped`) over SignalR (see [Event Model](./event-model.md)).

Response `200`:
```json
{ "id": "sim-90a...-guid", "status": "Running", "currentTick": 0, "startedAt": "2026-07-07T15:31:00Z" }
```
Status: `200`, `403`, `404`, `409` (illegal transition, e.g. resume when not paused).

### POST /api/v1/simulations/{id}/faults
Inject a fault into a running simulation.
Request:
```json
{ "faultType": "NodeFailed", "targetNodeId": "node-queue-1", "params": { "durationTicks": 20 } }
```
`faultType` maps to a fault-injection event (`FaultInjected`, `LatencyInjected`,
`PartitionCreated`, `PartitionHealed`). Response `202 Accepted`:
```json
{ "accepted": true, "faultId": "fault-77c...-guid", "atTick": 42 }
```
Status: `202`, `400`, `403`, `404`, `409` (simulation not running).

### GET /api/v1/simulations/{id}/events?fromSequence=
Replay/history endpoint for gap recovery and timeline scrubbing. Returns events with
`sequence >= fromSequence` (default `0`), ordered by `sequence`.
Response `200`:
```json
{
  "simulationId": "sim-90a...-guid",
  "fromSequence": 40,
  "count": 2,
  "events": [
    { "eventId": "e1", "simulationId": "sim-90a...-guid", "sequence": 41, "tick": 17, "occurredAt": "2026-07-07T15:31:03.100Z", "type": "MessagePublished", "sourceNodeId": "node-producer-1", "targetNodeId": "node-exchange-1", "correlationId": "msg-1", "traceId": "trace-1", "payload": { "routingKey": "order.created", "sizeBytes": 512 } },
    { "eventId": "e2", "simulationId": "sim-90a...-guid", "sequence": 42, "tick": 17, "occurredAt": "2026-07-07T15:31:03.150Z", "type": "MessageRouted", "sourceNodeId": "node-exchange-1", "targetNodeId": "node-queue-1", "correlationId": "msg-1", "traceId": "trace-1", "payload": { "routingKey": "order.created", "binding": "order.created" } }
  ]
}
```
Status: `200`, `403`, `404`.

### GET /api/v1/simulations/{id}/metrics
Response `200`: latest `MetricSnapshot` (or a series):
```json
{
  "simulationId": "sim-90a...-guid",
  "tick": 42,
  "throughput": 12.5,
  "avgLatencyMs": 84,
  "inFlight": 3,
  "dlqCount": 1,
  "retries": 4
}
```
Status: `200`, `403`, `404`.

## 5. Error model (RFC 7807)

All non-2xx responses use `application/problem+json`:
```json
{
  "type": "https://dfl.dev/problems/validation",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "detail": "Edge 'edge-2' references unknown targetNodeId 'node-queue-9'.",
  "instance": "/api/v1/scenarios",
  "errors": {
    "edges[1].targetNodeId": [ "Referenced node does not exist in the scenario." ]
  }
}
```

| Status | When |
|--------|------|
| `400 Bad Request` | Validation failure (FluentValidation), malformed topology. |
| `401 Unauthorized` | Missing/invalid bearer token. |
| `403 Forbidden` | Authenticated but not the owner of the resource. |
| `404 Not Found` | Resource does not exist. |
| `409 Conflict` | Illegal state transition, name conflict, referenced-in-use. |
| `422 Unprocessable Entity` | Semantically invalid but well-formed request. |
| `500 Internal Server Error` | Unexpected server fault. |

## Related documents

- [Event Model](./event-model.md)
- [WebSocket Events](./websocket-events.md)
- [Data Model](./data-model.md)
- [Components](./components.md)
- [Sequence Diagrams](./sequence-diagrams.md)
