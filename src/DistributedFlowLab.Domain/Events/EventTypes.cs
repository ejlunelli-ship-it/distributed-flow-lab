namespace DistributedFlowLab.Domain.Events;

/// <summary>
/// The canonical Event Catalog — every <see cref="Entities.SimulationEvent"/>
/// <c>Type</c> must be one of these names. Grouped exactly as in
/// .docs/02-architecture/event-model.md §3 and mirrored by the frontend
/// (`web/src/domain/events.ts`). Names are part of the wire contract: never rename.
/// </summary>
public static class EventTypes
{
    // Lifecycle (§3.1)
    public const string SimulationStarted = nameof(SimulationStarted);
    public const string SimulationPaused = nameof(SimulationPaused);
    public const string SimulationResumed = nameof(SimulationResumed);
    public const string SimulationStopped = nameof(SimulationStopped);
    public const string SimulationCompleted = nameof(SimulationCompleted);
    public const string TickAdvanced = nameof(TickAdvanced);

    // Node (§3.2)
    public const string NodeActivated = nameof(NodeActivated);
    public const string NodeStateChanged = nameof(NodeStateChanged);
    public const string NodeFailed = nameof(NodeFailed);
    public const string NodeRecovered = nameof(NodeRecovered);
    public const string ConsumerRegistered = nameof(ConsumerRegistered);

    // Messaging (§3.3)
    public const string MessagePublished = nameof(MessagePublished);
    public const string MessageRouted = nameof(MessageRouted);
    public const string MessageEnqueued = nameof(MessageEnqueued);
    public const string MessageDequeued = nameof(MessageDequeued);
    public const string MessageReceived = nameof(MessageReceived);
    public const string MessageProcessed = nameof(MessageProcessed);
    public const string AckReceived = nameof(AckReceived);
    public const string MessageNacked = nameof(MessageNacked);
    public const string RetryScheduled = nameof(RetryScheduled);
    public const string MessageRetried = nameof(MessageRetried);
    public const string DeadLettered = nameof(DeadLettered);
    public const string MessageExpired = nameof(MessageExpired);
    public const string MessageDropped = nameof(MessageDropped);

    // HTTP / RPC (§3.4)
    public const string HttpRequestStarted = nameof(HttpRequestStarted);
    public const string HttpResponseReceived = nameof(HttpResponseReceived);
    public const string HttpRequestFailed = nameof(HttpRequestFailed);
    public const string HttpRequestTimedOut = nameof(HttpRequestTimedOut);
    public const string GrpcCallStarted = nameof(GrpcCallStarted);
    public const string GrpcCallCompleted = nameof(GrpcCallCompleted);

    // Resilience / patterns (§3.5)
    public const string CircuitBreakerOpened = nameof(CircuitBreakerOpened);
    public const string CircuitBreakerHalfOpened = nameof(CircuitBreakerHalfOpened);
    public const string CircuitBreakerClosed = nameof(CircuitBreakerClosed);
    public const string SagaStarted = nameof(SagaStarted);
    public const string SagaStepCompleted = nameof(SagaStepCompleted);
    public const string SagaCompensationTriggered = nameof(SagaCompensationTriggered);
    public const string SagaCompleted = nameof(SagaCompleted);
    public const string CacheHit = nameof(CacheHit);
    public const string CacheMiss = nameof(CacheMiss);
    public const string CacheEvicted = nameof(CacheEvicted);

    // Fault injection (§3.6)
    public const string FaultInjected = nameof(FaultInjected);
    public const string LatencyInjected = nameof(LatencyInjected);
    public const string PartitionCreated = nameof(PartitionCreated);
    public const string PartitionHealed = nameof(PartitionHealed);

    /// <summary>
    /// Well-known source id for events originated by the engine itself
    /// (lifecycle and tick events), which have no owning node.
    /// </summary>
    public const string EngineSourceId = "engine";
}