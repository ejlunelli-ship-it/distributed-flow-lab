using DistributedFlowLab.Domain.Enums;
using DistributedFlowLab.Domain.Exceptions;
using DistributedFlowLab.Domain.ValueObjects;

namespace DistributedFlowLab.Domain.Entities;

/// <summary>
/// A running or completed execution instance of a <see cref="Scenario"/>.
/// Owns the lifecycle state machine (.docs/02-architecture/data-model.md §3.2):
/// illegal transitions throw <see cref="InvalidSimulationStateException"/> so the
/// engine and the API can never observe a half-applied state (ADR-007).
/// </summary>
public sealed class Simulation
{
    private Simulation(Guid id, Guid scenarioId, SimulationOptions options, DateTimeOffset createdAt)
    {
        Id = id;
        ScenarioId = scenarioId;
        Options = options;
        Status = SimulationStatus.Draft;
        CurrentTick = 0;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }

    public Guid ScenarioId { get; }

    public SimulationOptions Options { get; }

    public SimulationStatus Status { get; private set; }

    /// <summary>The engine's logical clock; advanced only by the tick loop.</summary>
    public int CurrentTick { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? EndedAt { get; private set; }

    public static Simulation Create(Guid scenarioId, SimulationOptions options, DateTimeOffset now)
        => new(Guid.NewGuid(), scenarioId, options, now);

    /// <summary>Draft → Running.</summary>
    public void Start(DateTimeOffset now)
    {
        EnsureStatus("start", SimulationStatus.Draft);
        Status = SimulationStatus.Running;
        StartedAt = now;
    }

    /// <summary>Running → Paused.</summary>
    public void Pause()
    {
        EnsureStatus("pause", SimulationStatus.Running);
        Status = SimulationStatus.Paused;
    }

    /// <summary>Paused → Running.</summary>
    public void Resume()
    {
        EnsureStatus("resume", SimulationStatus.Paused);
        Status = SimulationStatus.Running;
    }

    /// <summary>Running|Paused → Stopped (user-initiated, before natural completion).</summary>
    public void Stop(DateTimeOffset now)
    {
        EnsureStatus("stop", SimulationStatus.Running, SimulationStatus.Paused);
        Status = SimulationStatus.Stopped;
        EndedAt = now;
    }

    /// <summary>Running → Completed (all work drained / tick budget reached).</summary>
    public void Complete(DateTimeOffset now)
    {
        EnsureStatus("complete", SimulationStatus.Running);
        Status = SimulationStatus.Completed;
        EndedAt = now;
    }

    /// <summary>Running → Failed (fatal engine error).</summary>
    public void Fail(DateTimeOffset now)
    {
        EnsureStatus("fail", SimulationStatus.Running);
        Status = SimulationStatus.Failed;
        EndedAt = now;
    }

    /// <summary>Advances the logical clock by one tick. Only legal while Running.</summary>
    public int AdvanceTick()
    {
        EnsureStatus("advance tick", SimulationStatus.Running);
        return ++CurrentTick;
    }

    private void EnsureStatus(string attempted, params SimulationStatus[] allowed)
    {
        if (!allowed.Contains(Status))
        {
            throw new InvalidSimulationStateException(Id, Status, attempted);
        }
    }
}