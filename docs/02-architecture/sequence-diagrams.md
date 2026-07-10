# Sequence Diagrams

> Canonical runtime flows for Distributed Flow Lab. Every diagram uses the exact event names
> from the [Event Model](./event-model.md), the endpoints from
> [API Contracts](./api-contracts.md), and the hub methods from
> [WebSocket Events](./websocket-events.md). All backend events flow through the Event
> Dispatcher and SimulationHub; the client derives `AnimationStarted`/`AnimationFinished`
> locally and never invents state.

## 1. REST request flow

A generic REST call: thin Minimal API endpoint → FluentValidation → MediatR handler →
repository, returning a DTO or an RFC 7807 problem.

```mermaid
sequenceDiagram
    autonumber
    actor U as Learner
    participant W as Web SPA
    participant API as Minimal API (/api/v1)
    participant V as FluentValidation
    participant H as MediatR Handler
    participant R as Repository
    participant DB as PostgreSQL

    U->>W: Create scenario
    W->>API: POST /api/v1/scenarios
    API->>V: validate request
    alt invalid
        V-->>API: errors
        API-->>W: 400 problem+json
    else valid
        API->>H: CreateScenario command
        H->>R: persist scenario
        R->>DB: INSERT
        DB-->>R: ok
        R-->>H: scenario
        H-->>API: ScenarioDto
        API-->>W: 201 Created (Location)
    end
```

## 2. RabbitMQ: publish → route → enqueue → consume → ack

```mermaid
sequenceDiagram
    autonumber
    participant ENG as Simulation Engine
    participant RMQ as RabbitMQ Adapter
    participant DSP as Event Dispatcher
    participant HUB as SimulationHub
    participant W as Web SPA

    ENG->>RMQ: publish(message, routingKey)
    ENG->>DSP: MessagePublished (producer→exchange)
    RMQ->>RMQ: route by binding
    ENG->>DSP: MessageRouted (exchange→queue)
    ENG->>DSP: MessageEnqueued (queue)
    DSP->>HUB: ReceiveSimulationEvents(batch)
    HUB-->>W: [MessagePublished, MessageRouted, MessageEnqueued]
    ENG->>DSP: MessageDequeued (queue→consumer)
    ENG->>DSP: MessageReceived (consumer)
    ENG->>DSP: MessageProcessed (latencyMs)
    ENG->>DSP: AckReceived (consumer→queue)
    DSP->>HUB: ReceiveSimulationEvents(batch)
    HUB-->>W: [MessageDequeued, MessageReceived, MessageProcessed, AckReceived]
    W->>W: derive AnimationStarted/Finished per event
```

## 3. Kafka: produce → partition → consumer group → offset commit

```mermaid
sequenceDiagram
    autonumber
    participant ENG as Simulation Engine
    participant KFK as Kafka Adapter
    participant DSP as Event Dispatcher
    participant HUB as SimulationHub
    participant W as Web SPA

    ENG->>KFK: produce(message, key)
    ENG->>DSP: MessagePublished (producer→topic)
    KFK->>KFK: partition = hash(key) % partitions
    ENG->>DSP: MessageRouted (topic→partition)
    ENG->>DSP: MessageEnqueued (partition, offset)
    ENG->>DSP: ConsumerRegistered (consumer group)
    DSP->>HUB: ReceiveSimulationEvents(batch)
    HUB-->>W: [MessagePublished, MessageRouted, MessageEnqueued, ConsumerRegistered]
    ENG->>DSP: MessageDequeued (partition→consumer)
    ENG->>DSP: MessageReceived (consumer)
    ENG->>DSP: MessageProcessed (latencyMs)
    ENG->>DSP: AckReceived (offset commit)
    DSP->>HUB: ReceiveSimulationEvents(batch)
    HUB-->>W: [MessageDequeued, MessageReceived, MessageProcessed, AckReceived]
```

## 4. Retry with backoff

```mermaid
sequenceDiagram
    autonumber
    participant ENG as Simulation Engine
    participant DSP as Event Dispatcher
    participant HUB as SimulationHub
    participant W as Web SPA

    ENG->>DSP: MessageReceived (consumer)
    ENG->>DSP: MessageNacked (processing failed, requeue=true)
    ENG->>DSP: RetryScheduled (attempt=1, backoffMs=200, nextTick)
    DSP->>HUB: ReceiveSimulationEvents(batch)
    HUB-->>W: [MessageNacked, RetryScheduled]
    Note over ENG: waits until nextTick (TickAdvanced)
    ENG->>DSP: MessageRetried (attempt=1)
    ENG->>DSP: MessageReceived (consumer)
    ENG->>DSP: MessageProcessed (success)
    ENG->>DSP: AckReceived
    DSP->>HUB: ReceiveSimulationEvents(batch)
    HUB-->>W: [MessageRetried, MessageReceived, MessageProcessed, AckReceived]
```

## 5. Dead-letter (DLQ) path

```mermaid
sequenceDiagram
    autonumber
    participant ENG as Simulation Engine
    participant DSP as Event Dispatcher
    participant HUB as SimulationHub
    participant W as Web SPA

    loop attempts exhausted
        ENG->>DSP: MessageReceived (consumer)
        ENG->>DSP: MessageNacked (requeue=true)
        ENG->>DSP: RetryScheduled (attempt=n, backoffMs)
        ENG->>DSP: MessageRetried (attempt=n)
    end
    ENG->>DSP: MessageNacked (final, requeue=false)
    ENG->>DSP: DeadLettered (queue→deadLetterQueue, reason, attempts)
    ENG->>DSP: MessageEnqueued (deadLetterQueue)
    DSP->>HUB: ReceiveSimulationEvents(batch)
    HUB-->>W: [MessageNacked, DeadLettered, MessageEnqueued]
    W->>W: animate token queue→DLQ
```

## 6. Saga: steps and compensation

```mermaid
sequenceDiagram
    autonumber
    participant ENG as Simulation Engine (orchestrator)
    participant DSP as Event Dispatcher
    participant HUB as SimulationHub
    participant W as Web SPA

    ENG->>DSP: SagaStarted (sagaId, steps=[reserve, charge, ship])
    ENG->>DSP: SagaStepCompleted (step=reserve)
    ENG->>DSP: SagaStepCompleted (step=charge)
    DSP->>HUB: ReceiveSimulationEvents(batch)
    HUB-->>W: [SagaStarted, SagaStepCompleted x2]
    Note over ENG: step "ship" fails
    ENG->>DSP: SagaCompensationTriggered (failedStep=ship)
    ENG->>DSP: SagaStepCompleted (compensate charge)
    ENG->>DSP: SagaStepCompleted (compensate reserve)
    ENG->>DSP: SagaCompleted (outcome=compensated)
    DSP->>HUB: ReceiveSimulationEvents(batch)
    HUB-->>W: [SagaCompensationTriggered, SagaStepCompleted x2, SagaCompleted]
```

## 7. CQRS: command vs query and read-model update

```mermaid
sequenceDiagram
    autonumber
    actor U as Learner
    participant W as Web SPA
    participant API as Minimal API
    participant CMD as Command Handler (MediatR)
    participant WDB as Write store
    participant ENG as Simulation Engine
    participant DSP as Event Dispatcher
    participant MA as Metrics Aggregator (read model)
    participant HUB as SimulationHub

    rect rgb(230,240,255)
    Note over U,ENG: Command path (write)
    U->>W: Start simulation
    W->>API: POST /api/v1/simulations/{id}/start
    API->>CMD: StartSimulation command
    CMD->>WDB: persist status=Running
    CMD->>ENG: begin tick loop
    ENG->>DSP: SimulationStarted
    ENG->>DSP: MessageProcessed (latencyMs)
    end

    rect rgb(235,255,235)
    Note over DSP,HUB: Read-model update (derived)
    DSP->>MA: feed events
    MA->>MA: recompute MetricSnapshot
    DSP->>HUB: SimulationStateChanged / events
    end

    rect rgb(255,245,230)
    Note over U,MA: Query path (read)
    U->>W: View metrics
    W->>API: GET /api/v1/simulations/{id}/metrics
    API->>MA: GetMetrics query
    MA-->>API: MetricSnapshot
    API-->>W: 200 metrics
    end
```

## Related documents

- [Event Model](./event-model.md)
- [API Contracts](./api-contracts.md)
- [WebSocket Events](./websocket-events.md)
- [Components](./components.md)
- [System Overview](./system-overview.md)
