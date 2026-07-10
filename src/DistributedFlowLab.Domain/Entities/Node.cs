using DistributedFlowLab.Domain.Enums;
using DistributedFlowLab.Domain.Exceptions;
using DistributedFlowLab.Domain.ValueObjects;

namespace DistributedFlowLab.Domain.Entities;

/// <summary>
/// A participant in a <see cref="Scenario"/> topology (e.g. <c>node-producer-1</c>).
/// Identified by a string id that is stable within its scenario and referenced by
/// <see cref="Edge"/>s and by event <c>sourceNodeId</c>/<c>targetNodeId</c>.
/// </summary>
public sealed class Node
{
    public Node(string id, NodeType type, string label, Position position, IReadOnlyDictionary<string, object?>? config = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new InvalidScenarioException("Node id must be a non-empty string.");
        }

        Id = id;
        Type = type;
        Label = string.IsNullOrWhiteSpace(label) ? id : label;
        Position = position;
        Config = config ?? new Dictionary<string, object?>();
    }

    public string Id { get; }

    public NodeType Type { get; }

    public string Label { get; }

    public Position Position { get; }

    /// <summary>Type-specific settings (exchange type, TTL, partition count, …).</summary>
    public IReadOnlyDictionary<string, object?> Config { get; }
}