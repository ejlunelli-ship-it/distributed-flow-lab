using DistributedFlowLab.Domain.Entities;
using DistributedFlowLab.Domain.Enums;
using DistributedFlowLab.Domain.Exceptions;
using DistributedFlowLab.Domain.ValueObjects;

using FluentAssertions;

namespace DistributedFlowLab.Domain.Tests;

public sealed class SimulationTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 10, 12, 0, 0, TimeSpan.Zero);

    private static Simulation CreateDraft() =>
        Simulation.Create(Guid.NewGuid(), new SimulationOptions(), Now);

    [Fact]
    public void Create_starts_in_draft_at_tick_zero()
    {
        var simulation = CreateDraft();

        simulation.Status.Should().Be(SimulationStatus.Draft);
        simulation.CurrentTick.Should().Be(0);
        simulation.StartedAt.Should().BeNull();
        simulation.EndedAt.Should().BeNull();
    }

    [Fact]
    public void Start_transitions_draft_to_running_and_stamps_startedAt()
    {
        var simulation = CreateDraft();

        simulation.Start(Now);

        simulation.Status.Should().Be(SimulationStatus.Running);
        simulation.StartedAt.Should().Be(Now);
    }

    [Fact]
    public void Pause_and_resume_toggle_between_running_and_paused()
    {
        var simulation = CreateDraft();
        simulation.Start(Now);

        simulation.Pause();
        simulation.Status.Should().Be(SimulationStatus.Paused);

        simulation.Resume();
        simulation.Status.Should().Be(SimulationStatus.Running);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Stop_is_legal_from_running_and_paused(bool paused)
    {
        var simulation = CreateDraft();
        simulation.Start(Now);
        if (paused)
        {
            simulation.Pause();
        }

        simulation.Stop(Now);

        simulation.Status.Should().Be(SimulationStatus.Stopped);
        simulation.EndedAt.Should().Be(Now);
    }

    [Fact]
    public void Complete_transitions_running_to_completed()
    {
        var simulation = CreateDraft();
        simulation.Start(Now);

        simulation.Complete(Now);

        simulation.Status.Should().Be(SimulationStatus.Completed);
        simulation.EndedAt.Should().Be(Now);
    }

    [Fact]
    public void Fail_transitions_running_to_failed()
    {
        var simulation = CreateDraft();
        simulation.Start(Now);

        simulation.Fail(Now);

        simulation.Status.Should().Be(SimulationStatus.Failed);
    }

    [Fact]
    public void AdvanceTick_increments_only_while_running()
    {
        var simulation = CreateDraft();
        simulation.Start(Now);

        simulation.AdvanceTick().Should().Be(1);
        simulation.AdvanceTick().Should().Be(2);
        simulation.CurrentTick.Should().Be(2);
    }

    [Fact]
    public void Illegal_transitions_throw_InvalidSimulationStateException()
    {
        var simulation = CreateDraft();

        // From Draft: everything but start is illegal.
        simulation.Invoking(s => s.Pause()).Should().Throw<InvalidSimulationStateException>();
        simulation.Invoking(s => s.Resume()).Should().Throw<InvalidSimulationStateException>();
        simulation.Invoking(s => s.Stop(Now)).Should().Throw<InvalidSimulationStateException>();
        simulation.Invoking(s => s.Complete(Now)).Should().Throw<InvalidSimulationStateException>();
        simulation.Invoking(s => s.AdvanceTick()).Should().Throw<InvalidSimulationStateException>();

        // Start twice is illegal.
        simulation.Start(Now);
        simulation.Invoking(s => s.Start(Now)).Should().Throw<InvalidSimulationStateException>();

        // Resume while running is illegal.
        simulation.Invoking(s => s.Resume()).Should().Throw<InvalidSimulationStateException>();

        // Terminal states accept no further transitions.
        simulation.Stop(Now);
        simulation.Invoking(s => s.Start(Now)).Should().Throw<InvalidSimulationStateException>();
        simulation.Invoking(s => s.Pause()).Should().Throw<InvalidSimulationStateException>();
        simulation.Invoking(s => s.AdvanceTick()).Should().Throw<InvalidSimulationStateException>();
    }

    [Theory]
    [InlineData(0, 200)]
    [InlineData(10_001, 200)]
    [InlineData(100, -1)]
    [InlineData(100, 10_001)]
    public void Options_out_of_bounds_throw(int maxTicks, int tickIntervalMs)
    {
        var act = () => new SimulationOptions(maxTicks, tickIntervalMs);

        act.Should().Throw<InvalidScenarioException>();
    }
}