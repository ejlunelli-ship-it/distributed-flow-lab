using DistributedFlowLab.Application.Abstractions;
using DistributedFlowLab.Application.Dtos;

using MediatR;

namespace DistributedFlowLab.Application.Simulations;

/// <summary>Fetches a simulation with its current state (GET /api/v1/simulations/{id}).</summary>
public sealed record GetSimulationQuery(Guid SimulationId) : IRequest<SimulationDto>;

public sealed class GetSimulationQueryHandler : IRequestHandler<GetSimulationQuery, SimulationDto>
{
    private readonly ISimulationRepository _simulations;

    public GetSimulationQueryHandler(ISimulationRepository simulations)
    {
        _simulations = simulations;
    }

    public async Task<SimulationDto> Handle(GetSimulationQuery request, CancellationToken cancellationToken)
    {
        var simulation = await _simulations.GetAsync(request.SimulationId, cancellationToken)
            ?? throw new NotFoundException("Simulation", request.SimulationId);

        return SimulationDto.FromDomain(simulation);
    }
}