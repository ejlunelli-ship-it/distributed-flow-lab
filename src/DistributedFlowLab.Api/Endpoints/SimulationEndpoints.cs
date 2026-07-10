using DistributedFlowLab.Application.Dtos;
using DistributedFlowLab.Application.Simulations;

using MediatR;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DistributedFlowLab.Api.Endpoints;

/// <summary>
/// /api/v1/simulations — creation, lifecycle transitions, and the
/// events replay endpoint. Contracts: .docs/02-architecture/api-contracts.md §4.
/// </summary>
public static class SimulationEndpoints
{
    /// <summary>Body of POST /api/v1/simulations: { scenarioId, options? }.</summary>
    public sealed record CreateSimulationRequest(Guid ScenarioId, SimulationOptionsRequest? Options);

    public sealed record SimulationOptionsRequest(int? MaxTicks, int? TickIntervalMs);

    public static RouteGroupBuilder MapSimulationEndpoints(this RouteGroupBuilder group)
    {
        var simulations = group.MapGroup("/simulations");

        simulations.MapPost("/", CreateAsync);
        simulations.MapGet("/{id:guid}", GetAsync);
        simulations.MapPost("/{id:guid}/start", StartAsync);
        simulations.MapPost("/{id:guid}/pause", PauseAsync);
        simulations.MapPost("/{id:guid}/resume", ResumeAsync);
        simulations.MapPost("/{id:guid}/stop", StopAsync);
        simulations.MapGet("/{id:guid}/events", GetEventsAsync);

        return group;
    }

    private static async Task<Created<SimulationDto>> CreateAsync(
        [FromBody] CreateSimulationRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var simulation = await mediator.Send(
            new CreateSimulationCommand(request.ScenarioId, request.Options?.MaxTicks, request.Options?.TickIntervalMs),
            cancellationToken);
        return TypedResults.Created($"/api/v1/simulations/{simulation.Id}", simulation);
    }

    private static async Task<Ok<SimulationDto>> GetAsync(
        Guid id, IMediator mediator, CancellationToken cancellationToken)
        => TypedResults.Ok(await mediator.Send(new GetSimulationQuery(id), cancellationToken));

    private static async Task<Ok<SimulationDto>> StartAsync(
        Guid id, IMediator mediator, CancellationToken cancellationToken)
        => TypedResults.Ok(await mediator.Send(new StartSimulationCommand(id), cancellationToken));

    private static async Task<Ok<SimulationDto>> PauseAsync(
        Guid id, IMediator mediator, CancellationToken cancellationToken)
        => TypedResults.Ok(await mediator.Send(new PauseSimulationCommand(id), cancellationToken));

    private static async Task<Ok<SimulationDto>> ResumeAsync(
        Guid id, IMediator mediator, CancellationToken cancellationToken)
        => TypedResults.Ok(await mediator.Send(new ResumeSimulationCommand(id), cancellationToken));

    private static async Task<Ok<SimulationDto>> StopAsync(
        Guid id, IMediator mediator, CancellationToken cancellationToken)
        => TypedResults.Ok(await mediator.Send(new StopSimulationCommand(id), cancellationToken));

    private static async Task<Ok<SimulationEventsPageDto>> GetEventsAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken,
        [FromQuery] long fromSequence = 0)
        => TypedResults.Ok(await mediator.Send(new GetSimulationEventsQuery(id, fromSequence), cancellationToken));
}