using DistributedFlowLab.Application.Abstractions;
using DistributedFlowLab.Application.Dtos;

using MediatR;

namespace DistributedFlowLab.Application.Scenarios;

/// <summary>Fetches a scenario by id (GET /api/v1/scenarios/{id}).</summary>
public sealed record GetScenarioQuery(Guid ScenarioId) : IRequest<ScenarioDto>;

public sealed class GetScenarioQueryHandler : IRequestHandler<GetScenarioQuery, ScenarioDto>
{
    private readonly IScenarioRepository _scenarios;

    public GetScenarioQueryHandler(IScenarioRepository scenarios)
    {
        _scenarios = scenarios;
    }

    public async Task<ScenarioDto> Handle(GetScenarioQuery request, CancellationToken cancellationToken)
    {
        var scenario = await _scenarios.GetAsync(request.ScenarioId, cancellationToken)
            ?? throw new NotFoundException("Scenario", request.ScenarioId);

        return ScenarioDto.FromDomain(scenario);
    }
}