using DistributedFlowLab.Domain.Entities;

namespace DistributedFlowLab.Application.Abstractions;

/// <summary>
/// Port for the append-only per-simulation <c>Timeline</c> of
/// <see cref="SimulationEvent"/>s. Backs the replay/history endpoint
/// (GET /api/v1/simulations/{id}/events?fromSequence=).
/// </summary>
public interface IEventStore
{
    Task AppendAsync(SimulationEvent simulationEvent, CancellationToken cancellationToken);

    /// <summary>Returns events with <c>sequence &gt;= fromSequence</c>, ordered by sequence.</summary>
    Task<IReadOnlyList<SimulationEvent>> GetAsync(Guid simulationId, long fromSequence, CancellationToken cancellationToken);
}