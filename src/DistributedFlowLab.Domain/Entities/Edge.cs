using DistributedFlowLab.Domain.Exceptions;

namespace DistributedFlowLab.Domain.Entities;

/// <summary>
/// A directed connection between two <see cref="Node"/>s of a <see cref="Scenario"/>.
/// </summary>
public sealed class Edge
{
    public Edge(string id, string sourceNodeId, string targetNodeId, string? label = null, IReadOnlyDictionary<string, object?>? config = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new InvalidScenarioException("Edge id must be a non-empty string.");
        }

        if (string.IsNullOrWhiteSpace(sourceNodeId) || string.IsNullOrWhiteSpace(targetNodeId))
        {
            throw new InvalidScenarioException($"Edge '{id}' must reference both a source and a target node.");
        }

        if (sourceNodeId == targetNodeId)
        {
            throw new InvalidScenarioException($"Edge '{id}' cannot connect node '{sourceNodeId}' to itself.");
        }

        Id = id;
        SourceNodeId = sourceNodeId;
        TargetNodeId = targetNodeId;
        Label = label ?? string.Empty;
        Config = config ?? new Dictionary<string, object?>();
    }

    public string Id { get; }

    public string SourceNodeId { get; }

    public string TargetNodeId { get; }

    public string Label { get; }

    /// <summary>Edge-specific settings (routing key, weight, latency, …).</summary>
    public IReadOnlyDictionary<string, object?> Config { get; }
}