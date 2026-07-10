using DistributedFlowLab.Application.Dtos;
using DistributedFlowLab.Application.Scenarios;

using FluentAssertions;

namespace DistributedFlowLab.Application.Tests;

public sealed class CreateScenarioValidatorTests
{
    private readonly CreateScenarioCommandValidator _validator = new();

    private static NodeDto Node(string id, string type) =>
        new(id, type, id, new PositionDto(0, 0), null);

    [Fact]
    public void Valid_command_passes()
    {
        var command = new CreateScenarioCommand(
            "Order pipeline",
            null,
            "RabbitMQ",
            [Node("node-producer-1", "Producer"), Node("node-queue-1", "Queue")],
            [new EdgeDto("edge-1", "node-producer-1", "node-queue-1", null, null)]);

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Non_canonical_NodeType_is_rejected()
    {
        var command = new CreateScenarioCommand(
            "s", null, null, [Node("node-1", "Mainframe")], []);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("not a canonical NodeType"));
    }

    [Fact]
    public void Empty_name_is_rejected()
    {
        var command = new CreateScenarioCommand(string.Empty, null, null, [], []);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }
}