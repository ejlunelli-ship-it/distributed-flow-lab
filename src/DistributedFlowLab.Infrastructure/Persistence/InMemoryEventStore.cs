using System.Collections.Concurrent;

using DistributedFlowLab.Application.Abstractions;
using DistributedFlowLab.Domain.Entities;

namespace DistributedFlowLab.Infrastructure.Persistence;

/// <summary>
/// MVP in-memory append-only timeline, ordered by <c>sequence</c> per
/// simulation. Replaced by EF Core persistence in Version 1 so timelines
/// survive restarts (ADR-009 "sequence state must survive restarts").
/// </summary>
public sealed class InMemoryEventStore : IEventStore
{
    private readonly ConcurrentDictionary<Guid, List<SimulationEvent>> _timelines = new();

    public Task AppendAsync(SimulationEvent simulationEvent, CancellationToken cancellationToken)
    {
        var timeline = _timelines.GetOrAdd(simulationEvent.SimulationId, _ => []);
        lock (timeline)
        {
            timeline.Add(simulationEvent);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SimulationEvent>> GetAsync(Guid simulationId, long fromSequence, CancellationToken cancellationToken)
    {
        if (!_timelines.TryGetValue(simulationId, out var timeline))
        {
            return Task.FromResult<IReadOnlyList<SimulationEvent>>([]);
        }

        lock (timeline)
        {
            // Events are appended in sequence order by the single emitter,
            // so a filtered copy is already ordered.
            IReadOnlyList<SimulationEvent> page = timeline.Where(e => e.Sequence >= fromSequence).ToList();
            return Task.FromResult(page);
        }
    }
}