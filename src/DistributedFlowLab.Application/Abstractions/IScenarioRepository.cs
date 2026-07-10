using DistributedFlowLab.Domain.Entities;

namespace DistributedFlowLab.Application.Abstractions;

/// <summary>Port for <see cref="Scenario"/> persistence (in-memory in MVP; EF Core in V1).</summary>
public interface IScenarioRepository
{
    Task AddAsync(Scenario scenario, CancellationToken cancellationToken);

    Task<Scenario?> GetAsync(Guid scenarioId, CancellationToken cancellationToken);
}