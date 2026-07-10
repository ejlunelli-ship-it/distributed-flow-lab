using DistributedFlowLab.Application.Abstractions;
using DistributedFlowLab.Application.Dtos;
using DistributedFlowLab.Domain.Events;

using MediatR;

namespace DistributedFlowLab.Application.Simulations;

/// <summary>
/// Starts a Draft simulation (POST /api/v1/simulations/{id}/start): transitions
/// the aggregate, emits <c>SimulationStarted</c>, and hands the run to the
/// engine via <see cref="ISimulationScheduler"/>. The request returns
/// immediately; ticks proceed on the background loop (ADR-007).
/// </summary>
public sealed record StartSimulationCommand(Guid SimulationId) : IRequest<SimulationDto>;

public sealed class StartSimulationCommandHandler : IRequestHandler<StartSimulationCommand, SimulationDto>
{
    private readonly ISimulationRepository _simulations;
    private readonly IEventEmitter _events;
    private readonly ISimulationScheduler _scheduler;
    private readonly TimeProvider _timeProvider;

    public StartSimulationCommandHandler(
        ISimulationRepository simulations,
        IEventEmitter events,
        ISimulationScheduler scheduler,
        TimeProvider timeProvider)
    {
        _simulations = simulations;
        _events = events;
        _scheduler = scheduler;
        _timeProvider = timeProvider;
    }

    public async Task<SimulationDto> Handle(StartSimulationCommand request, CancellationToken cancellationToken)
    {
        var simulation = await _simulations.GetAsync(request.SimulationId, cancellationToken)
            ?? throw new NotFoundException("Simulation", request.SimulationId);

        var now = _timeProvider.GetUtcNow();
        simulation.Start(now);
        await _simulations.UpdateAsync(simulation, cancellationToken);

        await _events.EmitAsync(
            simulation.Id,
            simulation.CurrentTick,
            EventTypes.SimulationStarted,
            EventTypes.EngineSourceId,
            payload: new Dictionary<string, object?>
            {
                ["scenarioId"] = simulation.ScenarioId,
                ["startedAt"] = now,
            },
            cancellationToken: cancellationToken);

        await _scheduler.ScheduleAsync(simulation.Id, cancellationToken);

        return SimulationDto.FromDomain(simulation);
    }
}