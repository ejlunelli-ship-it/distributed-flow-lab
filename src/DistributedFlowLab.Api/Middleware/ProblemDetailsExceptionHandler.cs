using DistributedFlowLab.Application.Abstractions;
using DistributedFlowLab.Domain.Exceptions;

using FluentValidation;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DistributedFlowLab.Api.Middleware;

/// <summary>
/// Maps known exceptions to RFC 7807 problem+json responses
/// (api-contracts.md §5): validation → 400, not found → 404,
/// illegal state transition → 409. Everything else falls through to the
/// default 500 handler.
/// </summary>
public sealed class ProblemDetailsExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            ValidationException validation => new ProblemDetails
            {
                Type = "https://dfl.dev/problems/validation",
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
                Detail = "The request failed validation.",
                Extensions =
                {
                    ["errors"] = validation.Errors
                        .GroupBy(e => e.PropertyName, StringComparer.Ordinal)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray(), StringComparer.Ordinal),
                },
            },
            InvalidScenarioException invalidScenario => new ProblemDetails
            {
                Type = "https://dfl.dev/problems/validation",
                Title = "The scenario topology is invalid.",
                Status = StatusCodes.Status400BadRequest,
                Detail = invalidScenario.Message,
            },
            NotFoundException notFound => new ProblemDetails
            {
                Type = "https://dfl.dev/problems/not-found",
                Title = "Resource not found.",
                Status = StatusCodes.Status404NotFound,
                Detail = notFound.Message,
            },
            InvalidSimulationStateException invalidState => new ProblemDetails
            {
                Type = "https://dfl.dev/problems/invalid-state",
                Title = "Illegal simulation state transition.",
                Status = StatusCodes.Status409Conflict,
                Detail = invalidState.Message,
            },
            _ => null,
        };

        if (problem is null)
        {
            return false;
        }

        problem.Instance = httpContext.Request.Path;
        httpContext.Response.StatusCode = problem.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(problem, options: null, "application/problem+json", cancellationToken);
        return true;
    }
}