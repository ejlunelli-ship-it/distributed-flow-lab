using DistributedFlowLab.Application.Abstractions;
using DistributedFlowLab.Domain.Entities;
using DistributedFlowLab.Domain.Enums;
using DistributedFlowLab.Domain.Events;
using DistributedFlowLab.Domain.Exceptions;

using Microsoft.Extensions.Logging;

namespace DistributedFlowLab.Infrastructure.Engine;

/// <summary>
/// The tick loop of one simulation (ADR-007). Each iteration observes the
/// aggregate status at a safe boundary: Running advances the logical clock and
/// emits <c>TickAdvanced</c>; Paused waits without ticking; any terminal status
/// ends the run. Reaching the tick budget completes the run naturally with
/// <c>SimulationCompleted</c>. Correctness depends only on tick order — pacing
/// comes from the injected <see cref="ISimulationClock"/>, which is what makes
/// runs deterministic in tests (testing.md §5).
/// </summary>
public sealed partial class SimulationRunner
{
    private readonly ISimulationRepository _simulations;
    private readonly IEventEmitter _events;
    private readonly ISimulationClock _clock;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SimulationRunner> _logger;

    public SimulationRunner(
        ISimulationRepository simulations,
        IEventEmitter events,
        ISimulationClock clock,
        TimeProvider timeProvider,
        ILogger<SimulationRunner> logger)
    {
        _simulations = simulations;
        _events = events;
        _clock = clock;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task RunAsync(Guid simulationId, CancellationToken cancellationToken)
    {
        var simulation = await _simulations.GetAsync(simulationId, cancellationToken);
        if (simulation is null)
        {
            LogScheduledButMissing(simulationId);
            return;
        }

        try
        {
            await RunLoopAsync(simulation, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Host shutdown: leave the simulation as-is; the persisted timeline
            // ends at the last durable sequence (ADR-007 consequences).
        }
        catch (Exception ex)
        {
            LogRunFailed(ex, simulationId, simulation.CurrentTick);
            if (simulation.Status == SimulationStatus.Running)
            {
                simulation.Fail(_timeProvider.GetUtcNow());
                await _simulations.UpdateAsync(simulation, CancellationToken.None);
            }
        }
    }

    private async Task RunLoopAsync(Simulation simulation, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            switch (simulation.Status)
            {
                case SimulationStatus.Running:
                    if (!await TryAdvanceTickAsync(simulation, cancellationToken))
                    {
                        // A lifecycle command changed the status mid-iteration;
                        // loop around and re-observe at the boundary.
                        continue;
                    }

                    if (simulation.Status != SimulationStatus.Running)
                    {
                        continue;
                    }

                    await _clock.WaitForNextTickAsync(simulation.Options.TickIntervalMs, cancellationToken);
                    break;

                case SimulationStatus.Paused:
                    // Wait without advancing the clock; resume is observed on
                    // the next iteration (tick-boundary semantics, ADR-007).
                    await _clock.WaitForNextTickAsync(simulation.Options.TickIntervalMs, cancellationToken);
                    break;

                default:
                    // Stopped / Completed / Failed (or back to Draft): run ends.
                    return;
            }
        }
    }

    /// <summary>Advances one tick; returns false if a concurrent transition won the race.</summary>
    private async Task<bool> TryAdvanceTickAsync(Simulation simulation, CancellationToken cancellationToken)
    {
        int tick;
        try
        {
            tick = simulation.AdvanceTick();
        }
        catch (InvalidSimulationStateException)
        {
            return false;
        }

        await _simulations.UpdateAsync(simulation, cancellationToken);
        await _events.EmitAsync(
            simulation.Id,
            tick,
            EventTypes.TickAdvanced,
            EventTypes.EngineSourceId,
            payload: new Dictionary<string, object?> { ["tick"] = tick },
            cancellationToken: cancellationToken);

        if (tick >= simulation.Options.MaxTicks)
        {
            await CompleteAsync(simulation, tick, cancellationToken);
        }

        return true;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Simulation {SimulationId} was scheduled but no longer exists.")]
    private partial void LogScheduledButMissing(Guid simulationId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Simulation {SimulationId} failed at tick {Tick}.")]
    private partial void LogRunFailed(Exception exception, Guid simulationId, int tick);

    private async Task CompleteAsync(Simulation simulation, int totalTicks, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        try
        {
            simulation.Complete(now);
        }
        catch (InvalidSimulationStateException)
        {
            // Stopped concurrently by the user; the stop event already closed the run.
            return;
        }

        await _simulations.UpdateAsync(simulation, cancellationToken);
        await _events.EmitAsync(
            simulation.Id,
            totalTicks,
            EventTypes.SimulationCompleted,
            EventTypes.EngineSourceId,
            payload: new Dictionary<string, object?>
            {
                ["endedAt"] = now,
                ["totalTicks"] = totalTicks,
            },
            cancellationToken: cancellationToken);
    }
}