# CQRS Flow (Command Path vs Query Path with a Separate Read Model)

This diagram contrasts the two paths of **Command Query Responsibility Segregation (CQRS)**:
the **command path** mutates state and publishes domain events; the **query path** reads
from a separate, denormalized **read model** kept eventually consistent by a projection that
subscribes to those events. It teaches why reads and writes are separated and where
eventual consistency enters. CQRS is a V2 concept (canon §14) built from canonical node
types and events.

## Structure

```mermaid
flowchart LR
    Client[Client]
    subgraph Write["Command side (write model)"]
      CmdSvc[Service: command handler]
      WriteDB[(Database: write store)]
    end
    Broker[Broker / Exchange]
    subgraph Read["Query side (read model)"]
      Projector[Service: projector / consumer]
      ReadDB[(Database: read model)]
      QrySvc[Service: query handler]
    end

    Client -->|command| CmdSvc
    CmdSvc -->|persist| WriteDB
    CmdSvc -->|MessagePublished domain event| Broker
    Broker -->|MessageReceived| Projector
    Projector -->|update projection| ReadDB
    Client -->|query| QrySvc
    QrySvc -->|read| ReadDB
```

## Sequence (command → projection → query)

```mermaid
sequenceDiagram
    autonumber
    actor U as Client
    participant CMD as Command Service
    participant WDB as Write Store
    participant BRK as Broker
    participant PRJ as Projector
    participant RDB as Read Model
    participant QRY as Query Service

    Note over U,WDB: Command path (write)
    U->>CMD: HttpRequestStarted (command)
    CMD->>WDB: persist state change
    CMD->>BRK: MessagePublished (domain event)
    CMD-->>U: HttpResponseReceived (accepted)

    Note over BRK,RDB: Asynchronous projection (eventual consistency)
    BRK->>PRJ: MessageReceived (domain event)
    PRJ->>PRJ: MessageProcessed (build projection)
    PRJ->>RDB: update read model
    PRJ->>BRK: AckReceived

    Note over U,RDB: Query path (read)
    U->>QRY: HttpRequestStarted (query)
    QRY->>RDB: read denormalized view
    QRY-->>U: HttpResponseReceived (result)
```

## Legend & explanation

- **Command side.** A `Client` sends a command to a command `Service`, which validates,
  mutates the **write store** (`Database`), and publishes a domain event
  (`MessagePublished`) via a `Broker`/`Exchange`. In the backend this is the MediatR
  **command** path inside the Application layer ([ADR-004](../adr/ADR-004-clean-architecture.md)).
- **Projection.** A projector `Service` consumes the event (`MessageReceived` →
  `MessageProcessed` → `AckReceived`, canon §7) and updates the **read model** — a separate
  `Database` shaped for fast reads. The gap between the write commit and the read-model
  update is exactly the **eventual consistency** window the lesson makes visible.
- **Query side.** A `Client` query goes to a query `Service` that reads only from the read
  model, never touching the write store. In the backend this is the MediatR **query** path.
- **HTTP events.** `HttpRequestStarted` / `HttpResponseReceived` (canon §7) bracket the
  synchronous command and query calls, distinguishing them from the asynchronous messaging
  events on the projection path.
- **Why separate.** Reads and writes have different shapes, scaling needs, and consistency
  requirements; CQRS lets each be optimized independently, at the cost of the eventual-
  consistency delay the diagram highlights.

## Related documents

- [Message Flow](./message-flow.md)
- [Saga Flow](./saga-flow.md)
- [Architecture](../02-architecture/architecture.md)
- [ADR-004: Clean Architecture](../adr/ADR-004-clean-architecture.md)
- [Event Model](../02-architecture/event-model.md)
- [Diagrams Index](./README.md)
