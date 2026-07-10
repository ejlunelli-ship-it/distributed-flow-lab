namespace DistributedFlowLab.Application.Abstractions;

/// <summary>
/// Port for tick pacing (ADR-007, testing.md §5). The production implementation
/// waits wall-clock time between ticks; tests inject a clock that completes
/// immediately so engine runs are deterministic and instant.
/// </summary>
public interface ISimulationClock
{
    Task WaitForNextTickAsync(int tickIntervalMs, CancellationToken cancellationToken);
}