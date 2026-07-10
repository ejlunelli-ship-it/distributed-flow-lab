namespace DistributedFlowLab.Domain.Enums;

/// <summary>
/// Canonical vocabulary of node types a <see cref="Entities.Scenario"/> can contain.
/// Mirrored verbatim by the frontend (`web/src/domain/nodeType.ts`).
/// See .docs/02-architecture/data-model.md §3.1.
/// </summary>
public enum NodeType
{
    Producer,
    Consumer,
    Service,
    ApiGateway,
    LoadBalancer,
    Exchange,
    Queue,
    Topic,
    Partition,
    Broker,
    Database,
    Cache,
    DeadLetterQueue,
    Client,
}