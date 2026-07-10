# System Context Diagram (C4 Level 1)

This diagram places **Distributed Flow Lab (DFL)** in its environment: the human personas
who use it and the external infrastructure it depends on. At the context level DFL is a
single black box — an educational SaaS platform for learning distributed systems through
interactive visual simulations (canon §0). The external systems are the real infrastructure
brokers and datastore that give simulations production-grade fidelity (canon §2).

```mermaid
C4Context
    title DFL — System Context

    Person(beginner, "Beginner Developer", "Has written code but never operated a broker; needs intuition.")
    Person(backend, "Backend Engineer", "Builds services with queues and HTTP; reasons about resilience.")
    Person(architect, "Software Architect", "Designs topologies; validates and communicates trade-offs.")
    Person(instructor, "Instructor", "Teaches distributed systems; needs a live, safe teaching instrument.")
    Person(student, "Engineering Student", "Learns fundamentals formally; needs hands-on reinforcement.")

    System(dfl, "Distributed Flow Lab", "Compose architectures on a canvas and run simulations where every animation is driven by a real backend SimulationEvent.")

    System_Ext(rabbitmq, "RabbitMQ", "AMQP broker: exchanges, queues, routing keys, DLX. Backs Exchange/Queue/DeadLetterQueue nodes.")
    System_Ext(kafka, "Apache Kafka", "Log broker: topics, partitions, offsets, consumer groups. Backs Topic/Partition nodes.")
    System_Ext(redis, "Redis", "Cache + pub/sub. Backs Cache nodes and the SignalR backplane.")
    System_Ext(postgres, "PostgreSQL", "Relational store for Scenarios, Simulations, SimulationEvents, MetricSnapshots.")

    Rel(beginner, dfl, "Composes & runs simulations", "HTTPS / WSS")
    Rel(backend, dfl, "Explores resilience patterns", "HTTPS / WSS")
    Rel(architect, dfl, "Models & validates topologies", "HTTPS / WSS")
    Rel(instructor, dfl, "Teaches with live scenarios", "HTTPS / WSS")
    Rel(student, dfl, "Learns fundamentals hands-on", "HTTPS / WSS")

    Rel(dfl, rabbitmq, "Drives AMQP routing/ack/DLX behavior", "AMQP")
    Rel(dfl, kafka, "Drives topic/partition/offset behavior", "Kafka protocol")
    Rel(dfl, redis, "Drives cache behavior; SignalR backplane", "RESP / pub-sub")
    Rel(dfl, postgres, "Persists scenarios, events, metrics", "EF Core / TCP")
```

## Legend & explanation

- **Personas (left).** The five canonical personas (canon §11). All interact with DFL the
  same way — over HTTPS for REST and WSS for the realtime SignalR stream — but with
  different goals; see [Personas](../01-product/personas.md).
- **Distributed Flow Lab (center).** The system under design, treated as one box at this
  level. Its internal containers (Web SPA, API, Simulation Engine, SignalR hub, adapters)
  are decomposed in the [Container Diagram](./container-diagram.md).
- **External infrastructure (right).** RabbitMQ, Kafka, and Redis are real brokers used as
  **adapters** so simulated behavior reflects genuine broker semantics rather than a
  caricature (see [ADR-003](../adr/ADR-003-rabbitmq.md)). PostgreSQL persists all metadata
  and the replayable `SimulationEvent` timeline (canon §10).
- **Relationships.** Solid arrows are runtime dependencies; labels name the interaction and
  the protocol. The user↔DFL edges carry both REST (`/api/v1`) and realtime
  (`/hubs/simulation`) traffic (canon §8, §9).

Why the brokers are external systems: they are operationally independent processes that DFL
integrates with, not code DFL owns. Modeling them as external systems makes the fidelity
boundary explicit — DFL orchestrates real infrastructure and translates its observed
behavior into canonical events.

## Related documents

- [Container Diagram](./container-diagram.md)
- [Deployment Diagram](./deployment-diagram.md)
- [Architecture](../02-architecture/architecture.md)
- [Product Vision](../01-product/vision.md)
- [ADR-003: Real RabbitMQ adapter](../adr/ADR-003-rabbitmq.md)
- [Diagrams Index](./README.md)
