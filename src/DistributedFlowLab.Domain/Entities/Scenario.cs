using DistributedFlowLab.Domain.Exceptions;

namespace DistributedFlowLab.Domain.Entities;

/// <summary>
/// A saved architecture blueprint — a reusable topology of <see cref="Node"/>s and
/// <see cref="Edge"/>s that can be instantiated as <see cref="Simulation"/>s.
/// Topology invariants (unique ids, edges referencing existing nodes) are enforced
/// at construction so an invalid scenario can never exist.
/// </summary>
public sealed class Scenario
{
    private Scenario(Guid id, string name, string description, string conceptTag, IReadOnlyList<Node> nodes, IReadOnlyList<Edge> edges, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        Description = description;
        ConceptTag = conceptTag;
        Nodes = nodes;
        Edges = edges;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; }

    public string Name { get; }

    public string Description { get; }

    /// <summary>The concept this scenario teaches (e.g. "RabbitMQ").</summary>
    public string ConceptTag { get; }

    public IReadOnlyList<Node> Nodes { get; }

    public IReadOnlyList<Edge> Edges { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; }

    public static Scenario Create(string name, string description, string conceptTag, IReadOnlyList<Node> nodes, IReadOnlyList<Edge> edges, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidScenarioException("Scenario name must be a non-empty string.");
        }

        var nodeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in nodes)
        {
            if (!nodeIds.Add(node.Id))
            {
                throw new InvalidScenarioException($"Duplicate node id '{node.Id}'.");
            }
        }

        var edgeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var edge in edges)
        {
            if (!edgeIds.Add(edge.Id))
            {
                throw new InvalidScenarioException($"Duplicate edge id '{edge.Id}'.");
            }

            if (!nodeIds.Contains(edge.SourceNodeId))
            {
                throw new InvalidScenarioException($"Edge '{edge.Id}' references unknown sourceNodeId '{edge.SourceNodeId}'.");
            }

            if (!nodeIds.Contains(edge.TargetNodeId))
            {
                throw new InvalidScenarioException($"Edge '{edge.Id}' references unknown targetNodeId '{edge.TargetNodeId}'.");
            }
        }

        return new Scenario(Guid.NewGuid(), name.Trim(), description, conceptTag, nodes, edges, now);
    }
}