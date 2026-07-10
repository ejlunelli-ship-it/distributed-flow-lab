# Common Mistakes and Misconceptions

> Most distributed-systems knowledge is knowledge of what goes *wrong*. This document catalogs
> the mistakes engineers make most often. For each: **the mistake**, **why it hurts**, **the
> fix**, and **how DFL demonstrates it** — usually through `Fault Injection` so you can watch
> the failure unfold and then watch the fix hold.

Each entry names the exact `SimulationEvent`s and faults (`FaultInjected`, `LatencyInjected`,
`PartitionCreated`, `PartitionHealed`, `NodeFailed`) you use to reproduce it in a `Simulation`.

## 1. Assuming the network is reliable

**The mistake.** Writing code as if a call always reaches its target and always returns — no
timeouts, no handling of dropped connections or partitions (fallacy #1).

**Why it hurts.** Networks drop packets, sever connections, and partition. Code that assumes
reliability hangs forever on a lost response, leaks resources, and turns a transient blip into
an outage.

**The fix.** Treat every remote call as fallible: set **timeouts**, handle failure explicitly,
and design for retry + idempotency. Assume messages can be lost, delayed, duplicated, reordered.

**In DFL.** Inject `PartitionCreated` between a `Service` and its dependency, or `MessageDropped`
via a fault: requests without timeouts hang; those with timeouts surface `HttpRequestTimedOut`
and can react. Heal with `PartitionHealed` to see recovery.

## 2. Ignoring idempotency

**The mistake.** Assuming each message/request is processed exactly once, so processing has
non-idempotent side effects (charge a card, increment a counter) with no dedup.

**Why it hurts.** At-least-once delivery is the norm (see [Messaging Patterns](./messaging-patterns.md)).
A consumer that crashes after processing but before `AckReceived` gets the message **again** —
double charge, double email, corrupted counts.

**The fix.** Make processing **idempotent**: use a natural idempotency key / dedup store, upserts
instead of inserts, and conditional writes. Then a duplicate delivery is harmless.

**In DFL.** Kill a `Consumer` with `NodeFailed` after `MessageProcessed` but before
`AckReceived`; the broker redelivers (`MessageRetried`). A non-idempotent `Service` shows a
doubled effect; an idempotent one absorbs the duplicate. This directly demonstrates why
"exactly-once *delivery*" is a myth and idempotency is the real solution.

## 3. Unbounded retries (retry storms)

**The mistake.** Retrying failed operations with no cap, no backoff, and no jitter — often
"just add a retry loop".

**Why it hurts.** When a dependency is already struggling, synchronized, unbounded retries
**amplify** the load and prevent recovery — a self-inflicted DDoS known as a **retry storm**.
Retries also stack across layers (client retries × gateway retries × service retries).

**The fix.** Bound retries: a max attempt count, **exponential backoff + jitter**, and a circuit
breaker to stop retrying a dead dependency. Send exhausted messages to a DLQ.

**In DFL.** Configure a `Retry` policy with no cap and inject `NodeFailed` on the downstream:
`RetryScheduled`/`MessageRetried` events flood the `Timeline` and `MetricSnapshot.retries` and
`inFlight` spike. Add a cap + backoff and re-run to see the storm subside into orderly
`DeadLettered` overflow.

## 4. Missing a dead-letter queue

**The mistake.** No DLQ, so a message that cannot be processed is retried forever or silently
dropped.

**Why it hurts.** A single **poison message** blocks the queue head, starving all good messages
behind it, and burns CPU on doomed retries. Silently dropping loses data with no trace.

**The fix.** After N failed attempts (or on TTL expiry), **dead-letter** the message to a
`DeadLetterQueue` for inspection. The main flow keeps moving; nothing is lost silently.

**In DFL.** Publish a malformed message that always `MessageNacked`s. Without a DLQ, it loops
via `RetryScheduled` and blocks the queue. With a `DeadLetterQueue` node, exhausted retries emit
`DeadLettered` and `MetricSnapshot.dlqCount` rises while good traffic flows. See
[DLQ](../04-features/dlq.md).

## 5. Confusing at-least-once with exactly-once

**The mistake.** Believing a broker's "exactly-once" setting means side effects happen exactly
once end-to-end, so idempotency and dedup are skipped.

**Why it hurts.** Exactly-once *delivery* across an unreliable network is impossible. Vendor
"exactly-once" is scoped (e.g., Kafka transactions cover Kafka reads/writes, not your external
API call). Relying on it for external side effects yields duplicates in exactly the failure
cases you were trying to protect.

**The fix.** Design for **at-least-once + idempotent processing** = **effectively exactly-once
processing**. Treat vendor exactly-once as an optimization within its boundary, not a global
guarantee.

**In DFL.** The idempotency exercise (mistake #2) doubles as this lesson: force redelivery and
show that only idempotent processing yields a single effect, regardless of the "delivery
guarantee" label.

## 6. Tight coupling via synchronous calls

**The mistake.** Chaining services with blocking synchronous HTTP calls (A calls B calls C calls
D) for work that doesn't need an immediate answer.

**Why it hurts.** **Temporal coupling**: every service must be up simultaneously, and the slowest
link sets the latency. One slow/failed dependency blocks the whole chain and threads pile up —
cascading failure. Availability multiplies down (0.99⁴ ≈ 0.96).

**The fix.** Decouple with **asynchronous messaging** where a synchronous reply isn't required:
publish an event, let consumers process independently. Use timeouts + circuit breakers where you
must call synchronously.

**In DFL.** Build a synchronous `Service` chain and inject `LatencyInjected` on the last hop:
watch `HttpRequestStarted` pile up and `inFlight` climb across the whole chain. Rebuild with a
`Broker` decoupling the stages and show the front stays responsive while the queue absorbs the
slowdown.

## 7. Ignoring backpressure

**The mistake.** Assuming consumers keep up with producers, with no flow control, bounded
queues, or lag monitoring.

**Why it hurts.** When producers outpace consumers, queues grow unbounded — memory exhausts,
latency balloons, and eventually messages drop or the broker dies. The system "looks fine" right
up to collapse.

**The fix.** Apply **backpressure**: bounded queues, consumer-driven pull (Kafka lag),
prefetch/QoS limits, load shedding, and autoscaling consumers (competing consumers). Monitor
saturation (USE method) as a leading indicator.

**In DFL.** Set producer rate above consumer rate (or slow the consumer with `LatencyInjected`):
`MessageEnqueued` outruns `MessageDequeued`, `MetricSnapshot.inFlight` climbs, and at the limit
`MessageDropped`/`DeadLettered` appear. Add consumers to the queue (competing consumers) and
watch saturation fall. See [Observability](./observability.md) for the USE method.

## 8. The distributed monolith

**The mistake.** Splitting a system into services that are still tightly coupled — shared
database, synchronous chains, lock-step deploys — getting all the pain of distribution with none
of the benefit.

**Why it hurts.** You pay network latency, partial failure, and operational complexity, yet you
cannot deploy, scale, or fail independently. It is strictly worse than either a clean monolith
or true microservices.

**The fix.** Enforce **independence**: each service owns its data (no shared database), integrate
via well-defined async contracts/events, and ensure services can deploy and fail independently.
Draw the boundaries around business capabilities, not technical layers.

**In DFL.** Model multiple `Service`s pointing at a single shared `Database` with synchronous
`Edge`s. Inject `NodeFailed` on the database or `LatencyInjected` on one service and watch the
failure cascade to *all* services on the `Timeline` — proving they are not independent. Contrast
with an event-decoupled, database-per-service topology.

## 9. Ignoring partial failure

**The mistake.** Treating a call as binary success/failure, ignoring the case where *some* of a
multi-step operation succeeded and some didn't — and never handling the "slow, not dead"
(gray failure) case.

**Why it hurts.** Partial failure leaves the system in an **inconsistent intermediate state**
(stock reserved but payment failed). Gray failures defeat naive health checks: the node answers
pings but times out real work, so traffic keeps flowing to it.

**The fix.** Design for partial failure explicitly: **sagas with compensation** for multi-step
operations, timeouts that treat "slow" as "failed", and health checks that exercise real work.
Make operations idempotent and retryable so partial progress can be safely resumed.

**In DFL.** Run a `Saga` scenario and inject `FaultInjected` at a middle step: watch
`SagaStepCompleted` for earlier steps, then `SagaCompensationTriggered` unwinding them — the
system reconciling a partial failure. For gray failure, use `LatencyInjected` (not `NodeFailed`)
and watch a "healthy" node quietly poison the flow. See [Saga](../04-features/saga.md).

## 10. No observability

**The mistake.** Shipping distributed systems without structured logs, metrics, traces, or
correlation IDs — debugging by guesswork and SSH.

**Why it hurts.** In a distributed system you *cannot* reason about behavior from code alone.
Without correlated signals, an incident spanning five services is nearly impossible to diagnose;
mean-time-to-recovery skyrockets and root causes stay hidden.

**The fix.** Instrument from day one: **structured logs** (Serilog), **metrics** (RED/USE),
**distributed traces**, and propagate a **`traceId`**/**`correlationId`** on every hop. Make the
system explain itself.

**In DFL.** The platform is the antidote: every action is an authoritative `SimulationEvent` on
the `Timeline`, inspectable field-by-field, correlated by `traceId`/`correlationId`, and
aggregated into `MetricSnapshot`. Running any faulty scenario *with* the Timeline open shows how
observability turns an opaque failure into an obvious one. See [Observability](./observability.md).

## Summary

| Mistake | Core fix | Key DFL fault to reproduce |
|---------|----------|----------------------------|
| Network is reliable | Timeouts, retry, idempotency | `PartitionCreated`, `MessageDropped` |
| Ignoring idempotency | Idempotent processing / dedup | `NodeFailed` before `AckReceived` |
| Unbounded retries | Cap + backoff + jitter + breaker | `NodeFailed` + uncapped `Retry` |
| Missing DLQ | Dead-letter after N attempts | poison message, no `DeadLetterQueue` |
| At-least vs exactly-once | At-least-once + idempotency | forced `MessageRetried` |
| Synchronous coupling | Async messaging, timeouts | `LatencyInjected` on a sync chain |
| Ignoring backpressure | Bounded queues, pull, shedding | producer rate > consumer rate |
| Distributed monolith | Independence, DB per service | shared `Database` + `NodeFailed` |
| Ignoring partial failure | Saga + compensation, gray-failure aware | `FaultInjected` mid-saga |
| No observability | Logs + metrics + traces + IDs | run any fault with the `Timeline` |

## Related documents

- [Distributed Systems Primer](./distributed-systems.md)
- [Messaging Patterns](./messaging-patterns.md)
- [Architectural Patterns](./architectural-patterns.md)
- [Observability](./observability.md)
- [Hands-on Exercises](./exercises.md)
- [Retry](../04-features/retry.md)
- [DLQ](../04-features/dlq.md)
- [Saga](../04-features/saga.md)
- [Circuit Breaker](../04-features/circuit-breaker.md)
- [Glossary](../01-product/glossary.md)
- [Event Model](../02-architecture/event-model.md)
