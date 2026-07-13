using DistributedFlowLab.Application.Abstractions;
using DistributedFlowLab.Domain.Entities;
using DistributedFlowLab.Domain.Enums;
using DistributedFlowLab.Domain.Events;
using DistributedFlowLab.Domain.ValueObjects;
using DistributedFlowLab.Infrastructure.Engine;
using DistributedFlowLab.Infrastructure.Events;
using DistributedFlowLab.Infrastructure.Persistence;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

namespace DistributedFlowLab.Integration.Tests;

/// <summary>
/// Exercises the real engine pipeline (stores → sequenced emitter → tick loop)
/// with an immediate clock, proving the Sprint 1 exit criteria: lifecycle +
/// TickAdvanced events in order, gap-free monotonic sequence, and identical
/// event streams across runs (testing.md §5, NFR-8).
/// </summary>
public sealed class EngineDeterminismTests
{
    private sealed class ImmediateClock : ISimulationClock
    {
        public async Task WaitForNextTickAsync(int tickIntervalMs, CancellationToken cancellationToken)
            => await Task.Yield();
    }

    /// <summary>Transport-less publisher: the harness observes the store, not the wire.</summary>
    private sealed class NoopPublisher : IEventPublisher, ISimulationStatePublisher
    {
        public Task PublishAsync(SimulationEvent simulationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishStateAsync(Application.Dtos.SimulationStateDto state, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    /// <summary>One isolated engine pipeline over in-memory stores.</summary>
    private sealed class Harness
    {
        public InMemorySimulationRepository Simulations { get; } = new();

        public InMemoryEventStore EventStore { get; } = new();

        public SequencedEventEmitter Emitter { get; }

        public SimulationRunner Runner { get; }

        public Harness()
        {
            var publisher = new NoopPublisher();
            Emitter = new SequencedEventEmitter(EventStore, publisher, TimeProvider.System);
            Runner = new SimulationRunner(
                Simulations, Emitter, new ImmediateClock(), publisher, TimeProvider.System,
                NullLogger<SimulationRunner>.Instance);
        }

        /// <summary>Creates, starts, and runs a simulation to completion; returns its timeline.</summary>
        public async Task<IReadOnlyList<SimulationEvent>> RunToCompletionAsync(int maxTicks)
        {
            var simulation = Simulation.Create(
                Guid.NewGuid(), new SimulationOptions(maxTicks, tickIntervalMs: 0), DateTimeOffset.UtcNow);
            await Simulations.AddAsync(simulation, CancellationToken.None);

            // Mirror StartSimulationCommandHandler: transition, emit, run.
            simulation.Start(DateTimeOffset.UtcNow);
            await Emitter.EmitAsync(
                simulation.Id, 0, EventTypes.SimulationStarted, EventTypes.EngineSourceId,
                payload: new Dictionary<string, object?> { ["scenarioId"] = simulation.ScenarioId });

            await Runner.RunAsync(simulation.Id, CancellationToken.None);

            simulation.Status.Should().Be(SimulationStatus.Completed);
            return await EventStore.GetAsync(simulation.Id, 0, CancellationToken.None);
        }
    }

    [Fact]
    public async Task Run_emits_lifecycle_and_tick_events_in_order_with_gap_free_sequence()
    {
        const int maxTicks = 10;
        var timeline = await new Harness().RunToCompletionAsync(maxTicks);

        // Shape: SimulationStarted, TickAdvanced ×10, SimulationCompleted.
        timeline.Should().HaveCount(maxTicks + 2);
        timeline[0].Type.Should().Be(EventTypes.SimulationStarted);
        timeline[^1].Type.Should().Be(EventTypes.SimulationCompleted);
        timeline.Skip(1).Take(maxTicks).Should().OnlyContain(e => e.Type == EventTypes.TickAdvanced);

        // Monotonic, gap-free sequence starting at 0 (ADR-009).
        timeline.Select(e => e.Sequence).Should().BeEquivalentTo(
            Enumerable.Range(0, timeline.Count).Select(i => (long)i),
            options => options.WithStrictOrdering());

        // Ticks advance 1..maxTicks in order; completion is stamped with the final tick.
        timeline.Skip(1).Take(maxTicks).Select(e => e.Tick).Should().BeEquivalentTo(
            Enumerable.Range(1, maxTicks),
            options => options.WithStrictOrdering());
        timeline[^1].Tick.Should().Be(maxTicks);
    }

    [Fact]
    public async Task Two_runs_of_the_same_scenario_produce_identical_event_streams()
    {
        const int maxTicks = 25;
        var first = await new Harness().RunToCompletionAsync(maxTicks);
        var second = await new Harness().RunToCompletionAsync(maxTicks);

        // The deterministic projection of the envelope must match exactly
        // (identity fields — eventId, occurredAt, traceId — legitimately differ).
        var projection = (IReadOnlyList<SimulationEvent> events) =>
            events.Select(e => (e.Sequence, e.Tick, e.Type, e.SourceNodeId, e.TargetNodeId)).ToList();

        projection(second).Should().Equal(projection(first));
    }

    [Fact]
    public async Task Envelope_carries_all_canonical_fields()
    {
        var timeline = await new Harness().RunToCompletionAsync(3);

        timeline.Should().AllSatisfy(e =>
        {
            e.EventId.Should().NotBeEmpty();
            e.SimulationId.Should().NotBeEmpty();
            e.SourceNodeId.Should().Be(EventTypes.EngineSourceId);
            e.CorrelationId.Should().NotBeEmpty();
            e.TraceId.Should().NotBeEmpty();
            e.OccurredAt.Should().BeAfter(DateTimeOffset.MinValue);
            e.Payload.Should().NotBeNull();
        });
    }
}