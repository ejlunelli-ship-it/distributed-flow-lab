using DistributedFlowLab.Application.Abstractions;
using DistributedFlowLab.Application.Dtos;
using DistributedFlowLab.Domain.Events;

using MediatR;

namespace DistributedFlowLab.Application.Simulations;

/// <summary>
/// Stops a Running or Paused simulation before natural completion
/// (POST /api/v1/simulations/{id}/stop).
/// </summary>
public sealed record StopSimulationCommand(Guid SimulationId) : IRequest<SimulationDto>;

public sealed class StopSimulationCommandHandler : IRequestHandler<StopSimulationCommand, SimulationDto>
{
    private readonly ISimulationRepository _simulations;
    private readonly IEventEmitter _events;
    private readonly TimeProvider _timeProvider;

    public StopSimulationCommandHandler(
        ISimulationRepository simulations,
        IEventEmitter events,
        TimeProvider timeProvider)
    {
        _simulations = simulations;
        _events = events;
        _timeProvider = timeProvider;
    }

    public async Task<SimulationDto> Handle(StopSimulationCommand request, CancellationToken cancellationToken)
    {
        var simulation = await _simulations.GetAsync(request.SimulationId, cancellationToken)
            ?? throw new NotFoundException("Simulation", request.SimulationId);

        simulation.Stop(_timeProvider.GetUtcNow());
        await _simulations.UpdateAsync(simulation, cancellationToken);

        await _events.EmitAsync(
            simulation.Id,
            simulation.CurrentTick,
            EventTypes.SimulationStopped,
            EventTypes.EngineSourceId,
            payload: new Dictionary<string, object?>
            {
                ["atTick"] = simulation.CurrentTick,
                ["reason"] = "user",
            },
            cancellationToken: cancellationToken);

        return SimulationDto.FromDomain(simulation);
    }
}