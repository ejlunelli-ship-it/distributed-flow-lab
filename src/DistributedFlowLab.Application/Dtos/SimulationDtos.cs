using DistributedFlowLab.Domain.Entities;

namespace DistributedFlowLab.Application.Dtos;

/// <summary>
/// Simulation resource returned by /api/v1/simulations endpoints
/// (status serialized as the canonical string, e.g. "Running").
/// </summary>
public sealed record SimulationDto(
    Guid Id,
    Guid ScenarioId,
    string Status,
    int CurrentTick,
    int MaxTicks,
    int TickIntervalMs,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? EndedAt)
{
    public static SimulationDto FromDomain(Simulation simulation) => new(
        simulation.Id,
        simulation.ScenarioId,
        simulation.Status.ToString(),
        simulation.CurrentTick,
        simulation.Options.MaxTicks,
        simulation.Options.TickIntervalMs,
        simulation.CreatedAt,
        simulation.StartedAt,
        simulation.EndedAt);
}

/// <summary>The canonical event envelope on the wire (event-model.md §2).</summary>
public sealed record SimulationEventDto(
    Guid EventId,
    Guid SimulationId,
    long Sequence,
    int Tick,
    DateTimeOffset OccurredAt,
    string Type,
    string SourceNodeId,
    string? TargetNodeId,
    Guid CorrelationId,
    Guid TraceId,
    IReadOnlyDictionary<string, object?> Payload)
{
    public static SimulationEventDto FromDomain(SimulationEvent e) => new(
        e.EventId,
        e.SimulationId,
        e.Sequence,
        e.Tick,
        e.OccurredAt,
        e.Type,
        e.SourceNodeId,
        e.TargetNodeId,
        e.CorrelationId,
        e.TraceId,
        e.Payload);
}

/// <summary>
/// Response of GET /api/v1/simulations/{id}/events?fromSequence= — the replay
/// / gap-recovery contract (api-contracts.md §4).
/// </summary>
public sealed record SimulationEventsPageDto(
    Guid SimulationId,
    long FromSequence,
    int Count,
    IReadOnlyList<SimulationEventDto> Events);