using Microsoft.AspNetCore.SignalR;

namespace DistributedFlowLab.Infrastructure.Realtime;

/// <summary>
/// The realtime hub (canon §8, ADR-002), mapped at <c>/hubs/simulation</c> by
/// the Api host. Group model: exactly one SignalR group per
/// <c>simulationId</c> — clients subscribe to the simulations they observe and
/// receive only those events. Server→client pushes
/// (<c>ReceiveSimulationEvent</c>, <c>SimulationStateChanged</c>) are sent by
/// <see cref="SignalREventPublisher"/>, never by the hub itself.
/// </summary>
public sealed class SimulationHub : Hub
{
    /// <summary>Joins the caller to the group of one simulation.</summary>
    public Task Subscribe(string simulationId)
        => Groups.AddToGroupAsync(Context.ConnectionId, GroupName(simulationId));

    /// <summary>Removes the caller from the group of one simulation.</summary>
    public Task Unsubscribe(string simulationId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(simulationId));

    /// <summary>Canonical group name for a simulation (shared with the publisher).</summary>
    internal static string GroupName(string simulationId) => $"simulation:{simulationId}";
}