using DistributedFlowLab.Application.Dtos;

namespace DistributedFlowLab.Application.Abstractions;

/// <summary>
/// Port for pushing <c>SimulationStateChanged</c> to realtime observers when a
/// simulation's lifecycle status transitions (websocket-events.md §2). Called
/// by the lifecycle command handlers and by the engine on natural
/// completion/failure — never on plain tick advancement (ticks flow as
/// <c>TickAdvanced</c> events).
/// </summary>
public interface ISimulationStatePublisher
{
    Task PublishStateAsync(SimulationStateDto state, CancellationToken cancellationToken);
}