using System.Collections.Concurrent;

using DistributedFlowLab.Application.Abstractions;
using DistributedFlowLab.Domain.Entities;

namespace DistributedFlowLab.Infrastructure.Persistence;

/// <summary>
/// MVP in-memory scenario store. Replaced by the EF Core (PostgreSQL)
/// repository in Version 1 (backlog Epic 7).
/// </summary>
public sealed class InMemoryScenarioRepository : IScenarioRepository
{
    private readonly ConcurrentDictionary<Guid, Scenario> _scenarios = new();

    public Task AddAsync(Scenario scenario, CancellationToken cancellationToken)
    {
        _scenarios[scenario.Id] = scenario;
        return Task.CompletedTask;
    }

    public Task<Scenario?> GetAsync(Guid scenarioId, CancellationToken cancellationToken)
        => Task.FromResult(_scenarios.GetValueOrDefault(scenarioId));
}