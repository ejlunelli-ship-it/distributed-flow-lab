# Diagrams

This directory holds the canonical **Mermaid diagrams** for the Distributed Flow Lab (DFL).
They range from C4 structural views (context → container → deployment) down to concept-
specific runtime flows. Every diagram uses the canonical `NodeType`s, event names, and
contracts from the [Documentation Canon](../../CLAUDE.md) so that the same vocabulary holds
across all documentation.

The diagrams are meant to be read top-down: start with the structural views to understand
*what the system is*, then read the flow diagrams to understand *how it behaves at runtime*.

## Structural views (C4)

| Diagram | Description |
|---------|-------------|
| [System Context](./system-context.md) | C4 Level 1 — the five personas ↔ DFL ↔ external infrastructure (RabbitMQ, Kafka, Redis, PostgreSQL). |
| [Container Diagram](./container-diagram.md) | C4 Level 2 — Web SPA, API host, SignalR hub, Simulation Engine, messaging adapters, and PostgreSQL inside DFL. |
| [Deployment Diagram](./deployment-diagram.md) | Docker Compose topology — containers, shared network, ports, and named volumes for local dev. |

## Runtime flows

| Diagram | Description |
|---------|-------------|
| [Message Flow](./message-flow.md) | Generic end-to-end path: user action → REST → engine → SimulationEvents → SignalR → animation. |
| [RabbitMQ Flow](./rabbitmq-flow.md) | AMQP publish → exchange → routing key → queue → consumer → ack, plus the DLX dead-letter path. |
| [Kafka Flow](./kafka-flow.md) | Produce → topic → partitions → consumer group → offset commit; contrast with RabbitMQ. |
| [CQRS Flow](./cqrs-flow.md) | Command path vs query path with a separate, eventually consistent read model. |
| [Saga Flow](./saga-flow.md) | Orchestrated saga with steps and reverse-order compensation on failure. |

## Conventions

- Node shapes and names follow the canonical `NodeType` enum (canon §5): `Producer`,
  `Consumer`, `Service`, `Exchange`, `Queue`, `Topic`, `Partition`, `Broker`, `Database`,
  `Cache`, `DeadLetterQueue`, and the rest.
- Sequence-diagram messages are named with canonical **event names** from the Event Catalog
  (canon §7): `MessagePublished`, `MessageRouted`, `AckReceived`, `DeadLettered`,
  `SagaStepCompleted`, and so on.
- `AnimationStarted` / `AnimationFinished` are **client-derived presentation events**, never
  domain events (canon §7).

## Related documents

- [ADR Index](../adr/README.md)
- [Architecture](../02-architecture/architecture.md)
- [Event Model](../02-architecture/event-model.md)
- [RabbitMQ Feature](../04-features/rabbitmq.md)
- [Product Vision](../01-product/vision.md)
