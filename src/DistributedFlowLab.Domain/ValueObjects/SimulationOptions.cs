using DistributedFlowLab.Domain.Exceptions;

namespace DistributedFlowLab.Domain.ValueObjects;

/// <summary>
/// Runtime options of a <see cref="Entities.Simulation"/>
/// (see POST /api/v1/simulations in .docs/02-architecture/api-contracts.md).
/// <para>
/// <see cref="TickIntervalMs"/> = 0 runs the tick loop as fast as possible —
/// used by headless/deterministic tests (ADR-007: correctness depends on tick
/// order, never on wall-clock timing).
/// </para>
/// </summary>
public sealed record SimulationOptions
{
    public const int DefaultMaxTicks = 500;
    public const int DefaultTickIntervalMs = 200;
    public const int MaxAllowedTicks = 10_000;
    public const int MaxAllowedTickIntervalMs = 10_000;

    public SimulationOptions(int maxTicks = DefaultMaxTicks, int tickIntervalMs = DefaultTickIntervalMs)
    {
        if (maxTicks is < 1 or > MaxAllowedTicks)
        {
            throw new InvalidScenarioException($"maxTicks must be between 1 and {MaxAllowedTicks}.");
        }

        if (tickIntervalMs is < 0 or > MaxAllowedTickIntervalMs)
        {
            throw new InvalidScenarioException($"tickIntervalMs must be between 0 and {MaxAllowedTickIntervalMs}.");
        }

        MaxTicks = maxTicks;
        TickIntervalMs = tickIntervalMs;
    }

    /// <summary>Tick budget after which the run completes naturally (resource bound, canon §8 Security).</summary>
    public int MaxTicks { get; }

    /// <summary>Wall-clock pacing between ticks; 0 = unthrottled.</summary>
    public int TickIntervalMs { get; }
}