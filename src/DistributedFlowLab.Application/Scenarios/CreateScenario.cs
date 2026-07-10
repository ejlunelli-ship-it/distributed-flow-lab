using DistributedFlowLab.Application.Abstractions;
using DistributedFlowLab.Application.Dtos;
using DistributedFlowLab.Domain.Entities;
using DistributedFlowLab.Domain.Enums;
using DistributedFlowLab.Domain.ValueObjects;

using FluentValidation;

using MediatR;

namespace DistributedFlowLab.Application.Scenarios;

/// <summary>Creates a <see cref="Scenario"/> from a topology definition (POST /api/v1/scenarios).</summary>
public sealed record CreateScenarioCommand(
    string Name,
    string? Description,
    string? ConceptTag,
    IReadOnlyList<NodeDto> Nodes,
    IReadOnlyList<EdgeDto> Edges) : IRequest<ScenarioDto>;

public sealed class CreateScenarioCommandValidator : AbstractValidator<CreateScenarioCommand>
{
    public CreateScenarioCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Nodes).NotNull();
        RuleFor(c => c.Edges).NotNull();

        RuleForEach(c => c.Nodes).ChildRules(node =>
        {
            node.RuleFor(n => n.Id).NotEmpty();
            node.RuleFor(n => n.Type)
                .Must(type => Enum.TryParse<NodeType>(type, ignoreCase: false, out _))
                .WithMessage(n => $"'{n.Type}' is not a canonical NodeType.");
        });

        RuleForEach(c => c.Edges).ChildRules(edge =>
        {
            edge.RuleFor(e => e.Id).NotEmpty();
            edge.RuleFor(e => e.SourceNodeId).NotEmpty();
            edge.RuleFor(e => e.TargetNodeId).NotEmpty();
        });
    }
}

public sealed class CreateScenarioCommandHandler : IRequestHandler<CreateScenarioCommand, ScenarioDto>
{
    private readonly IScenarioRepository _scenarios;
    private readonly TimeProvider _timeProvider;

    public CreateScenarioCommandHandler(IScenarioRepository scenarios, TimeProvider timeProvider)
    {
        _scenarios = scenarios;
        _timeProvider = timeProvider;
    }

    public async Task<ScenarioDto> Handle(CreateScenarioCommand request, CancellationToken cancellationToken)
    {
        var nodes = request.Nodes
            .Select(n => new Node(
                n.Id,
                Enum.Parse<NodeType>(n.Type),
                n.Label,
                new Position(n.Position.X, n.Position.Y),
                n.Config))
            .ToList();

        var edges = request.Edges
            .Select(e => new Edge(e.Id, e.SourceNodeId, e.TargetNodeId, e.Label, e.Config))
            .ToList();

        var scenario = Scenario.Create(
            request.Name,
            request.Description ?? string.Empty,
            request.ConceptTag ?? string.Empty,
            nodes,
            edges,
            _timeProvider.GetUtcNow());

        await _scenarios.AddAsync(scenario, cancellationToken);

        return ScenarioDto.FromDomain(scenario);
    }
}