using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;

using DistributedFlowLab.Domain.Events;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;

namespace DistributedFlowLab.Integration.Tests;

/// <summary>
/// SignalR contract test (websocket-events.md, ADR-002): a real client
/// connects to /hubs/simulation, subscribes to a simulation's group, and must
/// receive every event via ReceiveSimulationEvent — in sequence order, in the
/// canonical envelope — plus SimulationStateChanged on lifecycle transitions.
/// </summary>
public sealed class SimulationHubContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SimulationHubContractTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private sealed record EventEnvelope(
        Guid EventId, Guid SimulationId, long Sequence, int Tick, DateTimeOffset OccurredAt,
        string Type, string SourceNodeId, string? TargetNodeId, Guid CorrelationId, Guid TraceId,
        Dictionary<string, object?> Payload);

    private sealed record StateDto(Guid SimulationId, string Status, int CurrentTick, DateTimeOffset UpdatedAt);

    private sealed record CreatedDto(Guid Id);

    private HubConnection BuildConnection() => new HubConnectionBuilder()
        .WithUrl(
            new Uri(_factory.Server.BaseAddress, "/hubs/simulation"),
            options => options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler())
        .Build();

    private async Task<Guid> CreateScenarioAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/scenarios", new
        {
            name = $"hub-contract-{Guid.NewGuid():N}",
            nodes = new object[]
            {
                new { id = "node-producer-1", type = "Producer", label = "p", position = new { x = 0, y = 0 }, config = new { } },
            },
            edges = Array.Empty<object>(),
        });
        return (await response.Content.ReadFromJsonAsync<CreatedDto>(Json))!.Id;
    }

    private async Task<Guid> CreateSimulationAsync(Guid scenarioId, int maxTicks)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/simulations", new
        {
            scenarioId,
            options = new { maxTicks, tickIntervalMs = 0 },
        });
        return (await response.Content.ReadFromJsonAsync<CreatedDto>(Json))!.Id;
    }

    [Fact]
    public async Task Subscribed_client_receives_all_events_in_sequence_order_with_state_changes()
    {
        const int maxTicks = 5;
        var scenarioId = await CreateScenarioAsync();
        var simulationId = await CreateSimulationAsync(scenarioId, maxTicks);

        var events = new ConcurrentQueue<EventEnvelope>();
        var states = new ConcurrentQueue<StateDto>();
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var connection = BuildConnection();
        connection.On<EventEnvelope>("ReceiveSimulationEvent", e =>
        {
            events.Enqueue(e);
            if (e.Type == EventTypes.SimulationCompleted)
            {
                completed.TrySetResult();
            }
        });
        connection.On<StateDto>("SimulationStateChanged", s => states.Enqueue(s));

        await connection.StartAsync();
        await connection.InvokeAsync("Subscribe", simulationId.ToString());

        (await _client.PostAsync($"/api/v1/simulations/{simulationId}/start", null))
            .EnsureSuccessStatusCode();

        await completed.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // Full timeline delivered: SimulationStarted + TickAdvanced×5 + SimulationCompleted.
        var received = events.ToList();
        received.Should().HaveCount(maxTicks + 2);
        received[0].Type.Should().Be(EventTypes.SimulationStarted);
        received[^1].Type.Should().Be(EventTypes.SimulationCompleted);

        // Delivered in sequence order, gap-free from 0 (ADR-009).
        received.Select(e => e.Sequence).Should().BeEquivalentTo(
            Enumerable.Range(0, received.Count).Select(i => (long)i),
            options => options.WithStrictOrdering());

        // Canonical envelope fields are populated.
        received.Should().AllSatisfy(e =>
        {
            e.EventId.Should().NotBeEmpty();
            e.SimulationId.Should().Be(simulationId);
            e.SourceNodeId.Should().Be(EventTypes.EngineSourceId);
            e.CorrelationId.Should().NotBeEmpty();
            e.TraceId.Should().NotBeEmpty();
        });

        // Lifecycle transitions were pushed as SimulationStateChanged.
        var statuses = states.Select(s => s.Status).ToList();
        statuses.Should().ContainInOrder("Running", "Completed");
        states.Should().AllSatisfy(s => s.SimulationId.Should().Be(simulationId));
    }

    [Fact]
    public async Task Unsubscribed_client_receives_nothing()
    {
        var scenarioId = await CreateScenarioAsync();
        var observedId = await CreateSimulationAsync(scenarioId, 3);
        var otherId = await CreateSimulationAsync(scenarioId, 3);

        var received = new ConcurrentQueue<EventEnvelope>();
        var observedCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var connection = BuildConnection();
        connection.On<EventEnvelope>("ReceiveSimulationEvent", e =>
        {
            received.Enqueue(e);
            if (e.Type == EventTypes.SimulationCompleted && e.SimulationId == observedId)
            {
                observedCompleted.TrySetResult();
            }
        });

        await connection.StartAsync();
        // Subscribe only to one of the two simulations (group isolation).
        await connection.InvokeAsync("Subscribe", observedId.ToString());

        (await _client.PostAsync($"/api/v1/simulations/{otherId}/start", null)).EnsureSuccessStatusCode();
        (await _client.PostAsync($"/api/v1/simulations/{observedId}/start", null)).EnsureSuccessStatusCode();

        await observedCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        received.Should().NotBeEmpty();
        received.Should().AllSatisfy(e => e.SimulationId.Should().Be(observedId));
    }
}