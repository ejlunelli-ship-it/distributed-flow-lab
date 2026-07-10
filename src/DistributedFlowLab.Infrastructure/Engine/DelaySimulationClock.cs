using DistributedFlowLab.Application.Abstractions;

namespace DistributedFlowLab.Infrastructure.Engine;

/// <summary>
/// Production tick pacing: waits wall-clock time between ticks. A floor of
/// 1 ms keeps an unthrottled loop (tickIntervalMs = 0) cooperative instead of
/// spinning a core. Deterministic tests replace this port with an immediate
/// clock (testing.md §5) — correctness never depends on wall-clock timing.
/// </summary>
public sealed class DelaySimulationClock : ISimulationClock
{
    public Task WaitForNextTickAsync(int tickIntervalMs, CancellationToken cancellationToken)
        => Task.Delay(Math.Max(1, tickIntervalMs), cancellationToken);
}