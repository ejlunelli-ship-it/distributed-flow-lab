using DistributedFlowLab.Domain.Entities;

namespace DistributedFlowLab.Application.Dtos;

/// <summary>Wire shape of a node position.</summary>
public sealed record PositionDto(int X, int Y);

/// <summary>Wire shape of a <see cref="Node"/> (see POST /api/v1/scenarios).</summary>
public sealed record NodeDto(
    string Id,
    string Type,
    string Label,
    PositionDto Position,
    Dictionary<string, object?>? Config);

/// <summary>Wire shape of an <see cref="Edge"/>.</summary>
public sealed record EdgeDto(
    string Id,
    string SourceNodeId,
    string TargetNodeId,
    string? Label,
    Dictionary<string, object?>? Config);

/// <summary>Full scenario resource returned by the API.</summary>
public sealed record ScenarioDto(
    Guid Id,
    string Name,
    string Description,
    string ConceptTag,
    IReadOnlyList<NodeDto> Nodes,
    IReadOnlyList<EdgeDto> Edges,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static ScenarioDto FromDomain(Scenario scenario) => new(
        scenario.Id,
        scenario.Name,
        scenario.Description,
        scenario.ConceptTag,
        scenario.Nodes.Select(n => new NodeDto(
            n.Id,
            n.Type.ToString(),
            n.Label,
            new PositionDto(n.Position.X, n.Position.Y),
            new Dictionary<string, object?>(n.Config))).ToList(),
        scenario.Edges.Select(e => new EdgeDto(
            e.Id,
            e.SourceNodeId,
            e.TargetNodeId,
            e.Label,
            new Dictionary<string, object?>(e.Config))).ToList(),
        scenario.CreatedAt,
        scenario.UpdatedAt);
}