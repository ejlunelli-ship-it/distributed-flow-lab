using DistributedFlowLab.Application.Abstractions;
using DistributedFlowLab.Application.Dtos;
using DistributedFlowLab.Application.Simulations;
using DistributedFlowLab.Domain.Entities;
using DistributedFlowLab.Domain.Enums;
using DistributedFlowLab.Domain.Events;
using DistributedFlowLab.Domain.Exceptions;
using DistributedFlowLab.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

using NSubstitute;

namespace DistributedFlowLab.Application.Tests;

public sealed class SimulationLifecycleHandlerTests
{
    private readonly ISimulationRepository _simulations = Substitute.For<ISimulationRepository>();
    private readonly IEventEmitter _events = Substitute.For<IEventEmitter>();
    private readonly ISimulationScheduler _scheduler = Substitute.For<ISimulationScheduler>();
    private readonly ISimulationStatePublisher _statePublisher = Substitute.For<ISimulationStatePublisher>();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero));

    private Simulation NewDraft()
    {
        var simulation = Simulation.Create(Guid.NewGuid(), new SimulationOptions(), _time.GetUtcNow());
        _simulations.GetAsync(simulation.Id, Arg.Any<CancellationToken>()).Returns(simulation);
        return simulation;
    }

    [Fact]
    public async Task Start_transitions_emits_SimulationStarted_and_schedules_the_run()
    {
        var simulation = NewDraft();
        var handler = new StartSimulationCommandHandler(_simulations, _events, _scheduler, _statePublisher, _time);

        var dto = await handler.Handle(new StartSimulationCommand(simulation.Id), CancellationToken.None);

        dto.Status.Should().Be("Running");
        simulation.Status.Should().Be(SimulationStatus.Running);
        await _events.Received(1).EmitAsync(
            simulation.Id,
            0,
            EventTypes.SimulationStarted,
            EventTypes.EngineSourceId,
            null,
            null,
            Arg.Any<IReadOnlyDictionary<string, object?>>(),
            Arg.Any<CancellationToken>());
        await _scheduler.Received(1).ScheduleAsync(simulation.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Pause_emits_SimulationPaused_with_atTick()
    {
        var simulation = NewDraft();
        simulation.Start(_time.GetUtcNow());
        var handler = new PauseSimulationCommandHandler(_simulations, _events, _statePublisher, _time);

        var dto = await handler.Handle(new PauseSimulationCommand(simulation.Id), CancellationToken.None);

        dto.Status.Should().Be("Paused");
        await _events.Received(1).EmitAsync(
            simulation.Id,
            simulation.CurrentTick,
            EventTypes.SimulationPaused,
            EventTypes.EngineSourceId,
            null,
            null,
            Arg.Is<IReadOnlyDictionary<string, object?>>(p => (int)p["atTick"]! == simulation.CurrentTick),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resume_emits_SimulationResumed()
    {
        var simulation = NewDraft();
        simulation.Start(_time.GetUtcNow());
        simulation.Pause();
        var handler = new ResumeSimulationCommandHandler(_simulations, _events, _statePublisher, _time);

        var dto = await handler.Handle(new ResumeSimulationCommand(simulation.Id), CancellationToken.None);

        dto.Status.Should().Be("Running");
        await _events.Received(1).EmitAsync(
            simulation.Id,
            simulation.CurrentTick,
            EventTypes.SimulationResumed,
            EventTypes.EngineSourceId,
            null,
            null,
            Arg.Any<IReadOnlyDictionary<string, object?>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Stop_emits_SimulationStopped_with_reason_user()
    {
        var simulation = NewDraft();
        simulation.Start(_time.GetUtcNow());
        var handler = new StopSimulationCommandHandler(_simulations, _events, _statePublisher, _time);

        var dto = await handler.Handle(new StopSimulationCommand(simulation.Id), CancellationToken.None);

        dto.Status.Should().Be("Stopped");
        await _events.Received(1).EmitAsync(
            simulation.Id,
            simulation.CurrentTick,
            EventTypes.SimulationStopped,
            EventTypes.EngineSourceId,
            null,
            null,
            Arg.Is<IReadOnlyDictionary<string, object?>>(p => (string)p["reason"]! == "user"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resume_when_not_paused_throws_InvalidSimulationStateException_and_emits_nothing()
    {
        var simulation = NewDraft();
        var handler = new ResumeSimulationCommandHandler(_simulations, _events, _statePublisher, _time);

        var act = () => handler.Handle(new ResumeSimulationCommand(simulation.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidSimulationStateException>();
        await _events.DidNotReceiveWithAnyArgs().EmitAsync(
            default, default, string.Empty, string.Empty, null, null, null, CancellationToken.None);
    }

    [Fact]
    public async Task Unknown_simulation_throws_NotFoundException()
    {
        var handler = new StartSimulationCommandHandler(_simulations, _events, _scheduler, _statePublisher, _time);

        var act = () => handler.Handle(new StartSimulationCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Lifecycle_transitions_push_SimulationStateChanged()
    {
        var simulation = NewDraft();
        var start = new StartSimulationCommandHandler(_simulations, _events, _scheduler, _statePublisher, _time);
        var stop = new StopSimulationCommandHandler(_simulations, _events, _statePublisher, _time);

        await start.Handle(new StartSimulationCommand(simulation.Id), CancellationToken.None);
        await stop.Handle(new StopSimulationCommand(simulation.Id), CancellationToken.None);

        await _statePublisher.Received(1).PublishStateAsync(
            Arg.Is<SimulationStateDto>(s => s.SimulationId == simulation.Id && s.Status == "Running"),
            Arg.Any<CancellationToken>());
        await _statePublisher.Received(1).PublishStateAsync(
            Arg.Is<SimulationStateDto>(s => s.SimulationId == simulation.Id && s.Status == "Stopped"),
            Arg.Any<CancellationToken>());
    }
}