# Saga Flow (Orchestration with Steps and Compensation)

This diagram traces an **orchestrated Saga**: a coordinator drives a multi-step distributed
transaction across services, and when a step fails it runs **compensating actions** to undo
the previously completed steps, restoring consistency without a distributed lock or 2PC. It
teaches how to keep multiple services consistent under partial failure. Saga is a V2 concept
(canon §14) and emits the canonical saga events `SagaStarted`, `SagaStepCompleted`,
`SagaCompensationTriggered`, `SagaCompleted` (canon §7).

## Happy path (all steps succeed)

```mermaid
sequenceDiagram
    autonumber
    participant O as Saga Orchestrator (Service)
    participant S1 as Order Service
    participant S2 as Payment Service
    participant S3 as Inventory Service

    O->>O: SagaStarted (correlationId)
    O->>S1: HttpRequestStarted (create order)
    S1-->>O: HttpResponseReceived (ok)
    O->>O: SagaStepCompleted (step=CreateOrder)
    O->>S2: HttpRequestStarted (charge payment)
    S2-->>O: HttpResponseReceived (ok)
    O->>O: SagaStepCompleted (step=ChargePayment)
    O->>S3: HttpRequestStarted (reserve stock)
    S3-->>O: HttpResponseReceived (ok)
    O->>O: SagaStepCompleted (step=ReserveStock)
    O->>O: SagaCompleted (status=Succeeded)
```

## Compensation path (a step fails)

```mermaid
sequenceDiagram
    autonumber
    participant O as Saga Orchestrator (Service)
    participant S1 as Order Service
    participant S2 as Payment Service
    participant S3 as Inventory Service

    O->>O: SagaStarted (correlationId)
    O->>S1: HttpRequestStarted (create order)
    S1-->>O: HttpResponseReceived (ok)
    O->>O: SagaStepCompleted (step=CreateOrder)
    O->>S2: HttpRequestStarted (charge payment)
    S2-->>O: HttpResponseReceived (ok)
    O->>O: SagaStepCompleted (step=ChargePayment)
    O->>S3: HttpRequestStarted (reserve stock)
    S3-->>O: HttpRequestFailed (out of stock)
    O->>O: SagaCompensationTriggered (from step=ChargePayment)
    O->>S2: HttpRequestStarted (refund payment — compensate ChargePayment)
    S2-->>O: HttpResponseReceived (refunded)
    O->>S1: HttpRequestStarted (cancel order — compensate CreateOrder)
    S1-->>O: HttpResponseReceived (cancelled)
    O->>O: SagaCompleted (status=Compensated)
```

## Legend & explanation

- **Orchestration style.** A dedicated orchestrator `Service` owns the workflow and issues
  each step, versus a choreography where services react to each other's events. DFL models
  the **orchestrated** variant because the control flow — and therefore the compensation
  order — is explicit and easy to visualize.
- **Steps.** Each successful step emits `SagaStepCompleted` (canon §7). Steps are invoked
  as service calls, bracketed by `HttpRequestStarted` / `HttpResponseReceived`; a failure is
  `HttpRequestFailed` (or `HttpRequestTimedOut`).
- **Compensation.** On failure the orchestrator emits `SagaCompensationTriggered` and then
  runs compensating actions **in reverse order** of the completed steps (refund before
  cancel), each undoing one prior step's effect. Compensation is the Saga's substitute for
  rollback in a system with no distributed transaction.
- **Terminal state.** The saga ends with `SagaCompleted`, whose status distinguishes a
  `Succeeded` run from a `Compensated` one — both are *successful* saga outcomes in the sense
  that the system is left consistent.
- **Correlation.** All events for one saga share a `correlationId` (canon §6) so the
  `Timeline` and inspector can group the whole distributed transaction, including its
  compensations.

## Related documents

- [Message Flow](./message-flow.md)
- [CQRS Flow](./cqrs-flow.md)
- [Event Model](../02-architecture/event-model.md)
- [Architecture](../02-architecture/architecture.md)
- [Diagrams Index](./README.md)
