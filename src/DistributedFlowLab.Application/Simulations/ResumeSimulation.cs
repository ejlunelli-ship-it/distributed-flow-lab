using DistributedFlowLab.Application.Abstractions;
using DistributedFlowLab.Application.Dtos;
using DistributedFlowLab.Domain.Events;

using MediatR;

namespace DistributedFlowLab.Application.Simulations;

/// <summary>Resumes a Paused simulation (POST /api/v1/simulations/{id}/resume).</summary>
public sealed record ResumeSimulationCommand(Guid SimulationId) : IRequest<SimulationDto>;

public sealed class ResumeSimulationCommandHandler : IRequestHandler<ResumeSimulationCommand, SimulationDto>
{
    private readonly ISimulationRepository _simulations;
    private readonly IEventEmitter _events;
    private readonly ISimulationStatePublisher _statePublisher;
    private readonly TimeProvider _timeProvider;

    public ResumeSimulationCommandHandler(
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

    public async Task<SimulationDto> Handle(ResumeSimulationCommand request, CancellationToken cancellationToken)
    {
        var simulation = await _simulations.GetAsync(request.SimulationId, cancellationToken)
            ?? throw new NotFoundException("Simulation", request.SimulationId);

        simulation.Resume();
        await _simulations.UpdateAsync(simulation, cancellationToken);

        await _events.EmitAsync(
            simulation.Id,
            simulation.CurrentTick,
            EventTypes.SimulationResumed,
            EventTypes.EngineSourceId,
            payload: new Dictionary<string, object?> { ["atTick"] = simulation.CurrentTick },
            cancellationToken: cancellationToken);

        await _statePublisher.PublishStateAsync(
            SimulationStateDto.FromDomain(simulation, _timeProvider.GetUtcNow()), cancellationToken);

        return SimulationDto.FromDomain(simulation);
    }
}