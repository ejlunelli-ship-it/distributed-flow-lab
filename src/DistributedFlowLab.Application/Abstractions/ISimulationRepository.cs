using DistributedFlowLab.Domain.Entities;

namespace DistributedFlowLab.Application.Abstractions;

/// <summary>Port for <see cref="Simulation"/> persistence (in-memory in MVP; EF Core in V1).</summary>
public interface ISimulationRepository
{
    Task AddAsync(Simulation simulation, CancellationToken cancellationToken);

    Task<Simulation?> GetAsync(Guid simulationId, CancellationToken cancellationToken);

    /// <summary>
    /// Persists state mutations of an already-tracked simulation. A no-op for the
    /// in-memory store (shared instance) but required so handlers stay
    /// persistence-agnostic when EF Core lands in V1.
    /// </summary>
    Task UpdateAsync(Simulation simulation, CancellationToken cancellationToken);
}