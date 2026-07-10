using DistributedFlowLab.Domain.Entities;

namespace DistributedFlowLab.Application.Abstractions;

/// <summary>
/// Port for pushing an already-sequenced <see cref="SimulationEvent"/> to
/// realtime transports. Implemented by the SignalR dispatcher in Sprint 2;
/// a no-op implementation is used until then.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync(SimulationEvent simulationEvent, CancellationToken cancellationToken);
}