using DistributedFlowLab.Application.Abstractions;
using DistributedFlowLab.Application.Dtos;
using DistributedFlowLab.Domain.Entities;
using DistributedFlowLab.Domain.ValueObjects;

using FluentValidation;

using MediatR;

namespace DistributedFlowLab.Application.Simulations;

/// <summary>
/// Creates a Draft <see cref="Simulation"/> from a scenario
/// (POST /api/v1/simulations).
/// </summary>
public sealed record CreateSimulationCommand(Guid ScenarioId, int? MaxTicks, int? TickIntervalMs)
    : IRequest<SimulationDto>;

public sealed class CreateSimulationCommandValidator : AbstractValidator<CreateSimulationCommand>
{
    public CreateSimulationCommandValidator()
    {
        RuleFor(c => c.ScenarioId).NotEmpty();
        RuleFor(c => c.MaxTicks)
            .InclusiveBetween(1, SimulationOptions.MaxAllowedTicks)
            .When(c => c.MaxTicks.HasValue);
        RuleFor(c => c.TickIntervalMs)
            .InclusiveBetween(0, SimulationOptions.MaxAllowedTickIntervalMs)
            .When(c => c.TickIntervalMs.HasValue);
    }
}

public sealed class CreateSimulationCommandHandler : IRequestHandler<CreateSimulationCommand, SimulationDto>
{
    private readonly IScenarioRepository _scenarios;
    private readonly ISimulationRepository _simulations;
    private readonly TimeProvider _timeProvider;

    public CreateSimulationCommandHandler(
        IScenarioRepository scenarios,
        ISimulationRepository simulations,
        TimeProvider timeProvider)
    {
        _scenarios = scenarios;
        _simulations = simulations;
        _timeProvider = timeProvider;
    }

    public async Task<SimulationDto> Handle(CreateSimulationCommand request, CancellationToken cancellationToken)
    {
        _ = await _scenarios.GetAsync(request.ScenarioId, cancellationToken)
            ?? throw new NotFoundException("Scenario", request.ScenarioId);

        var options = new SimulationOptions(
            request.MaxTicks ?? SimulationOptions.DefaultMaxTicks,
            request.TickIntervalMs ?? SimulationOptions.DefaultTickIntervalMs);

        var simulation = Simulation.Create(request.ScenarioId, options, _timeProvider.GetUtcNow());
        await _simulations.AddAsync(simulation, cancellationToken);

        return SimulationDto.FromDomain(simulation);
    }
}