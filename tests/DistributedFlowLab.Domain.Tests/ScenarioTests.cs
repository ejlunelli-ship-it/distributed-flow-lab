using DistributedFlowLab.Domain.Entities;
using DistributedFlowLab.Domain.Enums;
using DistributedFlowLab.Domain.Exceptions;
using DistributedFlowLab.Domain.ValueObjects;

using FluentAssertions;

namespace DistributedFlowLab.Domain.Tests;

public sealed class ScenarioTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 10, 12, 0, 0, TimeSpan.Zero);

    private static Node NewNode(string id, NodeType type = NodeType.Service) =>
        new(id, type, id, new Position(0, 0));

    [Fact]
    public void Create_builds_a_valid_topology()
    {
        var nodes = new[] { NewNode("node-producer-1", NodeType.Producer), NewNode("node-queue-1", NodeType.Queue) };
        var edges = new[] { new Edge("edge-1", "node-producer-1", "node-queue-1") };

        var scenario = Scenario.Create("Order pipeline", "desc", "RabbitMQ", nodes, edges, Now);

        scenario.Id.Should().NotBeEmpty();
        scenario.Nodes.Should().HaveCount(2);
        scenario.Edges.Should().HaveCount(1);
        scenario.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public void Duplicate_node_ids_are_rejected()
    {
        var nodes = new[] { NewNode("node-1"), NewNode("node-1") };

        var act = () => Scenario.Create("s", string.Empty, "REST", nodes, [], Now);

        act.Should().Throw<InvalidScenarioException>().WithMessage("*Duplicate node id*");
    }

    [Fact]
    public void Edge_referencing_unknown_node_is_rejected()
    {
        var nodes = new[] { NewNode("node-1") };
        var edges = new[] { new Edge("edge-1", "node-1", "node-ghost") };

        var act = () => Scenario.Create("s", string.Empty, "REST", nodes, edges, Now);

        act.Should().Throw<InvalidScenarioException>().WithMessage("*unknown targetNodeId*");
    }

    [Fact]
    public void Self_loop_edges_are_rejected()
    {
        var act = () => new Edge("edge-1", "node-1", "node-1");

        act.Should().Throw<InvalidScenarioException>().WithMessage("*itself*");
    }

    [Fact]
    public void Blank_scenario_name_is_rejected()
    {
        var act = () => Scenario.Create("  ", string.Empty, "REST", [], [], Now);

        act.Should().Throw<InvalidScenarioException>().WithMessage("*name*");
    }
}