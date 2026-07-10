namespace DistributedFlowLab.Application.Abstractions;

/// <summary>
/// Port through which the start command hands a Running simulation to the
/// engine runtime (the <c>BackgroundService</c> tick loop, ADR-007). The
/// starting HTTP request returns immediately; the run proceeds in background.
/// </summary>
public interface ISimulationScheduler
{
    ValueTask ScheduleAsync(Guid simulationId, CancellationToken cancellationToken);
}