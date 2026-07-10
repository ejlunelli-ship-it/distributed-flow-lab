using DistributedFlowLab.Application.Abstractions;
using DistributedFlowLab.Application.Dtos;

using MediatR;

namespace DistributedFlowLab.Application.Simulations;

/// <summary>
/// Replay/history query (GET /api/v1/simulations/{id}/events?fromSequence=):
/// returns events with <c>sequence &gt;= fromSequence</c> ordered by sequence,
/// enabling client gap recovery and deterministic replay (ADR-009).
/// </summary>
public sealed record GetSimulationEventsQuery(Guid SimulationId, long FromSequence = 0)
    : IRequest<SimulationEventsPageDto>;

public sealed class GetSimulationEventsQueryHandler
    : IRequestHandler<GetSimulationEventsQuery, SimulationEventsPageDto>
{
    private readonly ISimulationRepository _simulations;
    private readonly IEventStore _eventStore;

    public GetSimulationEventsQueryHandler(ISimulationRepository simulations, IEventStore eventStore)
    {
        _simulations = simulations;
        _eventStore = eventStore;
    }

    public async Task<SimulationEventsPageDto> Handle(GetSimulationEventsQuery request, CancellationToken cancellationToken)
    {
        _ = await _simulations.GetAsync(request.SimulationId, cancellationToken)
            ?? throw new NotFoundException("Simulation", request.SimulationId);

        var events = await _eventStore.GetAsync(request.SimulationId, request.FromSequence, cancellationToken);
        var dtos = events.Select(SimulationEventDto.FromDomain).ToList();

        return new SimulationEventsPageDto(request.SimulationId, request.FromSequence, dtos.Count, dtos);
    }
}