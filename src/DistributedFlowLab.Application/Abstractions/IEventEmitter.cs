using DistributedFlowLab.Domain.Entities;

namespace DistributedFlowLab.Application.Abstractions;

/// <summary>
/// The single gateway through which every <see cref="SimulationEvent"/> is born.
/// Implementations assign the envelope identity fields — <c>eventId</c>,
/// the monotonic gap-free per-simulation <c>sequence</c> (ADR-009), and
/// <c>occurredAt</c> — then append to the <see cref="IEventStore"/> and fan out
/// via <see cref="IEventPublisher"/>. Nothing else may mint a sequence.
/// </summary>
public interface IEventEmitter
{
    Task<SimulationEvent> EmitAsync(
        Guid simulationId,
        int tick,
        string type,
        string sourceNodeId,
        string? targetNodeId = null,
        Guid? correlationId = null,
        IReadOnlyDictionary<string, object?>? payload = null,
        CancellationToken cancellationToken = default);
}