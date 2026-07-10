using System.Collections.Concurrent;

using DistributedFlowLab.Application.Abstractions;
using DistributedFlowLab.Domain.Entities;

namespace DistributedFlowLab.Infrastructure.Events;

/// <summary>
/// The single place a <see cref="SimulationEvent"/> is minted. Serializes
/// emission per simulation so <c>sequence</c> is monotonic and gap-free
/// (ADR-009): assign identity → append to the store → fan out to transports,
/// all under the per-simulation lock, so store order and publish order agree.
/// </summary>
public sealed class SequencedEventEmitter : IEventEmitter
{
    private readonly IEventStore _eventStore;
    private readonly IEventPublisher _publisher;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();
    private readonly ConcurrentDictionary<Guid, long> _nextSequence = new();

    public SequencedEventEmitter(IEventStore eventStore, IEventPublisher publisher, TimeProvider timeProvider)
    {
        _eventStore = eventStore;
        _publisher = publisher;
        _timeProvider = timeProvider;
    }

    public async Task<SimulationEvent> EmitAsync(
        Guid simulationId,
        int tick,
        string type,
        string sourceNodeId,
        string? targetNodeId = null,
        Guid? correlationId = null,
        IReadOnlyDictionary<string, object?>? payload = null,
        CancellationToken cancellationToken = default)
    {
        var gate = _locks.GetOrAdd(simulationId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            var sequence = _nextSequence.GetOrAdd(simulationId, 0L);
            var eventId = Guid.NewGuid();

            var simulationEvent = new SimulationEvent(
                eventId,
                simulationId,
                sequence,
                tick,
                _timeProvider.GetUtcNow(),
                type,
                sourceNodeId,
                targetNodeId,
                // Message-related events pass the messageId; engine/lifecycle
                // events fall back to their own eventId so the field is always
                // a meaningful GUID.
                correlationId ?? eventId,
                // Real OpenTelemetry trace propagation lands in V1 (ADR-012).
                Guid.NewGuid(),
                payload ?? new Dictionary<string, object?>());

            await _eventStore.AppendAsync(simulationEvent, cancellationToken);
            await _publisher.PublishAsync(simulationEvent, cancellationToken);

            _nextSequence[simulationId] = sequence + 1;
            return simulationEvent;
        }
        finally
        {
            gate.Release();
        }
    }
}