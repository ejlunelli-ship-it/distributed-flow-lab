using DistributedFlowLab.Application.Dtos;
using DistributedFlowLab.Application.Scenarios;

using MediatR;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DistributedFlowLab.Api.Endpoints;

/// <summary>
/// /api/v1/scenarios — thin transport over MediatR (business logic never lives
/// here). Contracts: .docs/02-architecture/api-contracts.md §3.
/// </summary>
public static class ScenarioEndpoints
{
    public static RouteGroupBuilder MapScenarioEndpoints(this RouteGroupBuilder group)
    {
        var scenarios = group.MapGroup("/scenarios");

        scenarios.MapPost("/", CreateAsync);
        scenarios.MapGet("/{id:guid}", GetAsync);

        return group;
    }

    private static async Task<Created<ScenarioDto>> CreateAsync(
        [FromBody] CreateScenarioCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var scenario = await mediator.Send(command, cancellationToken);
        return TypedResults.Created($"/api/v1/scenarios/{scenario.Id}", scenario);
    }

    private static async Task<Ok<ScenarioDto>> GetAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var scenario = await mediator.Send(new GetScenarioQuery(id), cancellationToken);
        return TypedResults.Ok(scenario);
    }
}