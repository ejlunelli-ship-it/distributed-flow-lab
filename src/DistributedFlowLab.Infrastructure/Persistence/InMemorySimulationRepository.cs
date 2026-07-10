using System.Collections.Concurrent;

using DistributedFlowLab.Application.Abstractions;
using DistributedFlowLab.Domain.Entities;

namespace DistributedFlowLab.Infrastructure.Persistence;

/// <summary>
/// MVP in-memory simulation store. Handlers and the engine share the same
/// aggregate instance, so <see cref="UpdateAsync"/> is a no-op here; the EF
/// Core repository (V1) will persist the mutation.
/// </summary>
public sealed class InMemorySimulationRepository : ISimulationRepository
{
    private readonly ConcurrentDictionary<Guid, Simulation> _simulations = new();

    public Task AddAsync(Simulation simulation, CancellationToken cancellationToken)
    {
        _simulations[simulation.Id] = simulation;
        return Task.CompletedTask;
    }

    public Task<Simulation?> GetAsync(Guid simulationId, CancellationToken cancellationToken)
        => Task.FromResult(_simulations.GetValueOrDefault(simulationId));

    public Task UpdateAsync(Simulation simulation, CancellationToken cancellationToken)
        => Task.CompletedTask;
}