# Distributed Flow Lab — Product Vision

## Product Vision

**Distributed Flow Lab (DFL)** is an educational SaaS platform for learning distributed
systems through interactive visual simulations. Learners visually compose real-world
architectures — APIs, queues, brokers, databases, caches, and distributed services — on a
canvas, then run simulations in which **every animation is driven by a real backend event**.
The frontend never invents state; it renders truth produced by the simulation engine.

Distributed systems are notoriously hard to teach because their most important behaviors —
back-pressure, retries, dead-lettering, partitioning, eventual consistency, cascading
failure — are invisible in a code snippet and only emerge at runtime, under load, and under
failure. DFL makes these behaviors *visible, replayable, and safe to break*. The platform's
purpose is not to be a toy that "looks like" a broker; it is to **teach** distributed systems
by grounding every visual in an authoritative `SimulationEvent`.

## Mission

Our mission is to give every developer, architect, student, and instructor a place to
*see* distributed systems behave — correctly and incorrectly — so that the intuition normally
earned through years of production incidents can be built deliberately, visually, and in
minutes. We turn abstract patterns (Retry, DLQ, CQRS, Saga, Circuit Breaker) into concrete,
observable flows.

## Problem Statement

Distributed systems education suffers from a chronic gap between theory and behavior:

- **Diagrams are static.** Architecture diagrams show topology but not dynamics. They cannot
  express what happens when a `Consumer` slows down, a `Queue` fills, or a `Broker` partitions.
- **Real environments are expensive and risky to explore.** Spinning up RabbitMQ, Kafka, and
  Redis, generating meaningful load, and inducing controlled failures is time-consuming, and
  breaking things on purpose is unsafe in shared environments.
- **Text and video are passive.** Learners watch someone else explain a pattern instead of
  manipulating it, and there is no feedback loop tying an action to a system-level consequence.
- **Failure modes are the real curriculum, and they are hidden.** Most production knowledge is
  about what happens when things go wrong — but failure is exactly what conventional learning
  material under-represents.

DFL closes this gap by pairing a **visual composition canvas** with a **real event-driven
simulation engine** backed by actual broker semantics (AMQP exchanges/queues/DLX, Kafka
topics/partitions/offsets/consumer groups, Redis cache/pub-sub), and by exposing the resulting
event `Timeline` for inspection and replay.

## Goals

1. **Make dynamics visible.** Render live message flow, node state, and metrics as they are
   produced by backend `SimulationEvent`s — never fabricated on the client.
2. **Teach failure safely.** Provide first-class `Fault Injection` (latency, node failure,
   network partition) so learners can break systems deliberately and observe the consequences.
3. **Anchor to reality.** Use real infrastructure adapters (RabbitMQ, Kafka, Redis) so
   simulated behavior reflects genuine broker semantics rather than a simplified caricature.
4. **Enable replay and reflection.** Persist every event with a monotonic `sequence` so any
   `Simulation` can be scrubbed, replayed, and dissected after the fact.
5. **Provide a structured learning path.** Ship a `Catalog` of concept-focused `Scenario`s
   that progress from a single producer/consumer to full Saga and CQRS topologies.
6. **Be production-grade software.** Clean Architecture, SOLID, DDD where appropriate, an
   event-driven core, and full observability (OpenTelemetry, Serilog) — the platform is itself
   an exemplar of the discipline it teaches.

## Non-Goals

- **Not a production message broker or orchestrator.** DFL simulates and teaches; it is not a
  runtime for real business workloads.
- **Not a general-purpose infrastructure-as-code / deployment tool.** The canvas composes
  learning `Scenario`s, not deployable clusters.
- **Not a code sandbox / IDE.** DFL is about system-level behavior and event flow, not
  authoring application code.
- **Not a benchmarking suite.** Metrics are pedagogical (relative, explanatory), not
  authoritative performance benchmarks of the underlying brokers.
- **Not an offline desktop application (initially).** DFL targets the browser as a SaaS
  product; native/offline distribution is out of scope for the current horizon.

## Success Metrics

Success is measured against concrete, instrumented KPIs. All product analytics are derived
from real usage events, consistent with the platform's event-driven philosophy.

### Learning outcome KPIs
| KPI | Target |
|-----|--------|
| Concept comprehension lift (pre/post quiz on a lesson) | +30 percentage points average |
| Scenario completion rate (learners who run a Scenario to `SimulationCompleted`) | ≥ 70% |
| Fault-injection engagement (learners who inject ≥ 1 fault per session) | ≥ 50% |
| Catalog breadth per learner (distinct concept tags explored in first month) | ≥ 5 |

### Engagement KPIs
| KPI | Target |
|-----|--------|
| Week-4 retention (returning learners) | ≥ 35% |
| Median session length | ≥ 12 minutes |
| Simulations run per active user per week | ≥ 4 |
| Instructor-created custom `Scenario`s (V3 classrooms) | ≥ 3 per active instructor / term |

### Product quality KPIs
| KPI | Target |
|-----|--------|
| Event-to-animation latency (P95, server emit → client render) | ≤ 250 ms |
| Simulation event delivery reliability (no observable `sequence` gaps) | ≥ 99.9% |
| Canvas interaction frame rate under a 500-node scenario | ≥ 50 fps |
| Crash-free session rate | ≥ 99.5% |

## Long-Term Vision

DFL evolves from a single-player learning tool into a **collaborative, extensible ecosystem for
distributed-systems education**:

- **Guided curricula and assessments** (V3): structured lessons, exercises, and graded
  assessments layered on top of the simulation engine.
- **Multi-user classrooms** (V3): instructors run live sessions, assign `Scenario`s, and review
  learner `Timeline`s.
- **Collaboration and marketplace** (Future): real-time co-editing of `Scenario`s and a
  community marketplace of shared lessons and topologies.
- **Plugin SDK for custom nodes** (Future): partners and educators extend the `NodeType`
  vocabulary and simulation behaviors without forking the platform.
- **Cloud multi-tenant SaaS** (Future): organization-level tenancy, SSO, and seat management for
  bootcamps and enterprises.

The north star: DFL becomes the default place engineers go to *understand* — not just read
about — how distributed systems behave.

## Target Audience

DFL serves the full canonical persona set defined in the shared canon:

- **Beginner Developer** — has written code but never operated a broker; needs intuition.
- **Backend Engineer** — builds services with queues and HTTP; needs to reason about resilience.
- **Software Architect** — designs topologies; needs to communicate and validate trade-offs.
- **Instructor** — teaches distributed systems; needs a live, safe teaching instrument.
- **Engineering Student** — learning fundamentals formally; needs hands-on reinforcement.

Each persona's goals, pain points, and how DFL helps are detailed in
[Personas](./personas.md).

## Educational Objectives

Every feature must answer: *"What should the student learn?"* Concretely, after using DFL a
learner should be able to:

1. **Explain messaging fundamentals** — distinguish a `Producer`, `Consumer`, `Queue`,
   `Exchange`, `Topic`, and `Routing Key`, and predict how a `Message` flows through them.
2. **Reason about delivery guarantees** — describe acknowledgement (`AckReceived`), negative
   acknowledgement (`MessageNacked`), redelivery (`MessageRetried`), and dead-lettering
   (`DeadLettered`), and when each occurs.
3. **Contrast RabbitMQ and Kafka** — routing/exchanges/queues versus topics/partitions/offsets
   and consumer groups, and the consistency and ordering implications of each.
4. **Apply resilience patterns** — Retry with backoff, DLQ, Circuit Breaker, and understand the
   failure they mitigate.
5. **Understand orchestration and consistency** — Saga (with compensation), CQRS, Event
   Sourcing, and the difference between eventual and strong consistency.
6. **Diagnose failure** — use `Fault Injection` and the `Timeline` to observe, then articulate,
   cascading failure, back-pressure, and partition behavior.

These objectives map directly to the concept `Catalog` and to the learning content described
in the 06-learning documentation set.

## Related documents

- [Product Requirements Document](./prd.md)
- [Personas](./personas.md)
- [Roadmap](./roadmap.md)
- [Backlog](./backlog.md)
- [Glossary](./glossary.md)
- [Architecture](../02-architecture/architecture.md)
- [Event Model](../02-architecture/event-model.md)
