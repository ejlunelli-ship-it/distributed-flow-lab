using DistributedFlowLab.Application.Abstractions;
using DistributedFlowLab.Application.Dtos;
using DistributedFlowLab.Domain.Events;

using MediatR;

namespace DistributedFlowLab.Application.Simulations;

/// <summary>
/// Pauses a Running simulation (POST /api/v1/simulations/{id}/pause).
/// The tick loop observes the new status at the next tick boundary (ADR-007).
/// </summary>
public sealed record PauseSimulationCommand(Guid SimulationId) : IRequest<SimulationDto>;

public sealed class PauseSimulationCommandHandler : IRequestHandler<PauseSimulationCommand, SimulationDto>
{
    private readonly ISimulationRepository _simulations;
    private readonly IEventEmitter _events;
    private readonly ISimulationStatePublisher _statePublisher;
    private readonly TimeProvider _timeProvider;

    public PauseSimulationCommandHandler(
        ISimulationRepository simulations,
        IEventEmitter events,
        ISimulationStatePublisher statePublisher,
        TimeProvider timeProvider)
    {
        _simulations = simulations;
        _events = events;
        _statePublisher = statePublisher;
        _timeProvider = timeProvider;
    }

    public async Task<SimulationDto> Handle(PauseSimulationCommand request, CancellationToken cancellationToken)
    {
        var simulation = await _simulations.GetAsync(request.SimulationId, cancellationToken)
            ?? throw new NotFoundException("Simulation", request.SimulationId);

        simulation.Pause();
        await _simulations.UpdateAsync(simulation, cancellationToken);

        await _events.EmitAsync(
            simulation.Id,
            simulation.CurrentTick,
            EventTypes.SimulationPaused,
            EventTypes.EngineSourceId,
            payload: new Dictionary<string, object?> { ["atTick"] = simulation.CurrentTick },
            cancellationToken: cancellationToken);

        await _statePublisher.PublishStateAsync(
            SimulationStateDto.FromDomain(simulation, _timeProvider.GetUtcNow()), cancellationToken);

        return SimulationDto.FromDomain(simulation);
    }
}