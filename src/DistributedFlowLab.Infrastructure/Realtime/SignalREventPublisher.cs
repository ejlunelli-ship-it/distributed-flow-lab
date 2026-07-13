using DistributedFlowLab.Application.Abstractions;
using DistributedFlowLab.Application.Dtos;
using DistributedFlowLab.Domain.Entities;

using Microsoft.AspNetCore.SignalR;

namespace DistributedFlowLab.Infrastructure.Realtime;

/// <summary>
/// The SignalR event dispatcher (ADR-002): pushes each already-sequenced
/// <see cref="SimulationEvent"/> to the per-simulation group as
/// <c>ReceiveSimulationEvent</c>, and lifecycle transitions as
/// <c>SimulationStateChanged</c>. Publish order matches store order because
/// the <see cref="Events.SequencedEventEmitter"/> calls this under the
/// per-simulation lock (ADR-009); batched <c>ReceiveSimulationEvents</c>
/// arrives in V1 (backlog Epic 4).
/// </summary>
public sealed class SignalREventPublisher : IEventPublisher, ISimulationStatePublisher
{
    private readonly IHubContext<SimulationHub> _hub;

    public SignalREventPublisher(IHubContext<SimulationHub> hub)
    {
        _hub = hub;
    }

    public Task PublishAsync(SimulationEvent simulationEvent, CancellationToken cancellationToken)
        => _hub.Clients
            .Group(SimulationHub.GroupName(simulationEvent.SimulationId.ToString()))
            .SendAsync(
                "ReceiveSimulationEvent",
                SimulationEventDto.FromDomain(simulationEvent),
                cancellationToken);

    public Task PublishStateAsync(SimulationStateDto state, CancellationToken cancellationToken)
        => _hub.Clients
            .Group(SimulationHub.GroupName(state.SimulationId.ToString()))
            .SendAsync("SimulationStateChanged", state, cancellationToken);
}