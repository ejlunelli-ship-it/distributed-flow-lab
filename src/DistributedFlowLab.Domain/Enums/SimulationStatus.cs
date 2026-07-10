namespace DistributedFlowLab.Domain.Enums;

/// <summary>
/// Lifecycle states of a <see cref="Entities.Simulation"/>.
/// Legal transitions (see .docs/02-architecture/data-model.md §3.2):
/// Draft → Running; Running ⇄ Paused; Running|Paused → Stopped;
/// Running → Completed; Running → Failed.
/// </summary>
public enum SimulationStatus
{
    Draft,
    Running,
    Paused,
    Completed,
    Stopped,
    Failed,
}