using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using DistributedFlowLab.Domain.Events;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DistributedFlowLab.Integration.Tests;

/// <summary>
/// End-to-end REST contract test: scenario → simulation → start → natural
/// completion → events replay, plus RFC 7807 error mapping
/// (api-contracts.md §4–§5).
/// </summary>
public sealed class SimulationLifecycleApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;

    public SimulationLifecycleApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private sealed record ScenarioResponse(Guid Id, string Name);

    private sealed record SimulationResponse(Guid Id, Guid ScenarioId, string Status, int CurrentTick);

    private sealed record EventResponse(
        Guid EventId, Guid SimulationId, long Sequence, int Tick, string Type,
        string SourceNodeId, string? TargetNodeId);

    private sealed record EventsPageResponse(Guid SimulationId, long FromSequence, int Count, List<EventResponse> Events);

    private async Task<Guid> CreateScenarioAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/scenarios", new
        {
            name = "Order pipeline",
            description = "Producer to queue",
            conceptTag = "RabbitMQ",
            nodes = new object[]
            {
                new { id = "node-producer-1", type = "Producer", label = "Order API", position = new { x = 40, y = 120 }, config = new { } },
                new { id = "node-queue-1", type = "Queue", label = "orders.q", position = new { x = 480, y = 120 }, config = new { } },
            },
            edges = new object[]
            {
                new { id = "edge-1", sourceNodeId = "node-producer-1", targetNodeId = "node-queue-1", label = "publish", config = new { } },
            },
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var scenario = await response.Content.ReadFromJsonAsync<ScenarioResponse>(Json);
        return scenario!.Id;
    }

    private async Task<SimulationResponse> CreateSimulationAsync(Guid scenarioId, int maxTicks)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/simulations", new
        {
            scenarioId,
            options = new { maxTicks, tickIntervalMs = 0 },
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<SimulationResponse>(Json))!;
    }

    [Fact]
    public async Task Full_lifecycle_start_to_completion_produces_an_ordered_gap_free_timeline()
    {
        const int maxTicks = 5;
        var scenarioId = await CreateScenarioAsync();
        var simulation = await CreateSimulationAsync(scenarioId, maxTicks);
        simulation.Status.Should().Be("Draft");

        var start = await _client.PostAsync($"/api/v1/simulations/{simulation.Id}/start", null);
        start.StatusCode.Should().Be(HttpStatusCode.OK);

        // The run proceeds on the background engine; poll until it completes.
        var completed = await PollUntilStatusAsync(simulation.Id, "Completed", TimeSpan.FromSeconds(10));
        completed.CurrentTick.Should().Be(maxTicks);

        var page = (await _client.GetFromJsonAsync<EventsPageResponse>(
            $"/api/v1/simulations/{simulation.Id}/events?fromSequence=0", Json))!;

        page.Count.Should().Be(maxTicks + 2);
        page.Events[0].Type.Should().Be(EventTypes.SimulationStarted);
        page.Events[^1].Type.Should().Be(EventTypes.SimulationCompleted);
        page.Events.Skip(1).Take(maxTicks).Should().OnlyContain(e => e.Type == EventTypes.TickAdvanced);
        page.Events.Select(e => e.Sequence).Should().BeEquivalentTo(
            Enumerable.Range(0, page.Count).Select(i => (long)i),
            options => options.WithStrictOrdering());

        // fromSequence replay returns exactly the suffix (gap recovery, ADR-009).
        var suffix = (await _client.GetFromJsonAsync<EventsPageResponse>(
            $"/api/v1/simulations/{simulation.Id}/events?fromSequence=3", Json))!;
        suffix.Events.Should().HaveCount(page.Count - 3);
        suffix.Events[0].Sequence.Should().Be(3);
    }

    [Fact]
    public async Task Pause_and_resume_emit_lifecycle_events_and_preserve_order()
    {
        var scenarioId = await CreateScenarioAsync();
        // Slow ticks so the simulation is still running when we pause.
        var response = await _client.PostAsJsonAsync("/api/v1/simulations", new
        {
            scenarioId,
            options = new { maxTicks = 10_000, tickIntervalMs = 50 },
        });
        var simulation = (await response.Content.ReadFromJsonAsync<SimulationResponse>(Json))!;

        (await _client.PostAsync($"/api/v1/simulations/{simulation.Id}/start", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var paused = await _client.PostAsync($"/api/v1/simulations/{simulation.Id}/pause", null);
        paused.StatusCode.Should().Be(HttpStatusCode.OK);
        (await paused.Content.ReadFromJsonAsync<SimulationResponse>(Json))!.Status.Should().Be("Paused");

        var resumed = await _client.PostAsync($"/api/v1/simulations/{simulation.Id}/resume", null);
        (await resumed.Content.ReadFromJsonAsync<SimulationResponse>(Json))!.Status.Should().Be("Running");

        var stopped = await _client.PostAsync($"/api/v1/simulations/{simulation.Id}/stop", null);
        (await stopped.Content.ReadFromJsonAsync<SimulationResponse>(Json))!.Status.Should().Be("Stopped");

        var page = (await _client.GetFromJsonAsync<EventsPageResponse>(
            $"/api/v1/simulations/{simulation.Id}/events", Json))!;

        var types = page.Events.Select(e => e.Type).ToList();
        types.Should().ContainInOrder(
            EventTypes.SimulationStarted,
            EventTypes.SimulationPaused,
            EventTypes.SimulationResumed,
            EventTypes.SimulationStopped);
        page.Events.Select(e => e.Sequence).Should().BeInAscendingOrder();
        page.Events.Select(e => e.Sequence).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Illegal_transition_returns_409_problem_json()
    {
        var scenarioId = await CreateScenarioAsync();
        var simulation = await CreateSimulationAsync(scenarioId, 5);

        // Resume a Draft simulation: illegal.
        var response = await _client.PostAsync($"/api/v1/simulations/{simulation.Id}/resume", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Unknown_simulation_returns_404_problem_json()
    {
        var response = await _client.PostAsync($"/api/v1/simulations/{Guid.NewGuid()}/start", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Invalid_scenario_topology_returns_400_problem_json()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/scenarios", new
        {
            name = "broken",
            nodes = new object[]
            {
                new { id = "node-1", type = "Producer", label = "p", position = new { x = 0, y = 0 }, config = new { } },
            },
            edges = new object[]
            {
                new { id = "edge-1", sourceNodeId = "node-1", targetNodeId = "node-ghost", label = "", config = new { } },
            },
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    private async Task<SimulationResponse> PollUntilStatusAsync(Guid simulationId, string status, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            var simulation = await _client.GetFromJsonAsync<SimulationResponse>(
                $"/api/v1/simulations/{simulationId}", Json);
            if (simulation!.Status == status)
            {
                return simulation;
            }

            await Task.Delay(25);
        }

        throw new TimeoutException($"Simulation {simulationId} did not reach status '{status}' within {timeout}.");
    }
}