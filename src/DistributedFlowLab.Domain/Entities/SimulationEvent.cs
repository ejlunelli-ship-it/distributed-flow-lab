namespace DistributedFlowLab.Domain.Entities;

/// <summary>
/// The canonical event envelope — the unit of truth for everything the frontend
/// renders (ADR-006, ADR-009). <c>Sequence</c> is monotonic and gap-free per
/// simulation and is *the* ordering key; <c>Tick</c> groups events on the logical
/// clock; <c>OccurredAt</c> is display-only. Shape mirrors
/// .docs/02-architecture/event-model.md §2 exactly (camelCase on the wire).
/// </summary>
public sealed record SimulationEvent
{
    public SimulationEvent(
        Guid eventId,
        Guid simulationId,
        long sequence,
        int tick,
        DateTimeOffset occurredAt,
        string type,
        string sourceNodeId,
        string? targetNodeId,
        Guid correlationId,
        Guid traceId,
        IReadOnlyDictionary<string, object?> payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceNodeId);
        ArgumentOutOfRangeException.ThrowIfNegative(sequence);
        ArgumentOutOfRangeException.ThrowIfNegative(tick);

        EventId = eventId;
        SimulationId = simulationId;
        Sequence = sequence;
        Tick = tick;
        OccurredAt = occurredAt;
        Type = type;
        SourceNodeId = sourceNodeId;
        TargetNodeId = targetNodeId;
        CorrelationId = correlationId;
        TraceId = traceId;
        Payload = payload;
    }

    public Guid EventId { get; }

    public Guid SimulationId { get; }

    /// <summary>Monotonic, gap-free, per-simulation total order (ADR-009).</summary>
    public long Sequence { get; }

    /// <summary>Engine logical clock at emission time.</summary>
    public int Tick { get; }

    /// <summary>Wall-clock timestamp — for display/diagnostics, never ordering.</summary>
    public DateTimeOffset OccurredAt { get; }

    /// <summary>One of the <see cref="Events.EventTypes"/> catalog names.</summary>
    public string Type { get; }

    public string SourceNodeId { get; }

    /// <summary>Destination node; null for node-local events.</summary>
    public string? TargetNodeId { get; }

    /// <summary>The messageId a message-related event belongs to.</summary>
    public Guid CorrelationId { get; }

    /// <summary>Cross-subsystem trace correlation (OpenTelemetry).</summary>
    public Guid TraceId { get; }

    /// <summary>Type-specific fields (see the Event Catalog per-event payload docs).</summary>
    public IReadOnlyDictionary<string, object?> Payload { get; }
}