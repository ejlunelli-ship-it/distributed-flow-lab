using DistributedFlowLab.Application.Abstractions;
using DistributedFlowLab.Domain.Entities;

namespace DistributedFlowLab.Infrastructure.Events;

/// <summary>
/// Placeholder transport until the SignalR dispatcher lands in Sprint 2
/// (Epic 4). Events are still persisted to the <see cref="IEventStore"/> and
/// fully observable via GET /api/v1/simulations/{id}/events.
/// </summary>
public sealed class NullEventPublisher : IEventPublisher
{
    public Task PublishAsync(SimulationEvent simulationEvent, CancellationToken cancellationToken)
        => Task.CompletedTask;
}