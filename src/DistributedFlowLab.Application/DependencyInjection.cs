using DistributedFlowLab.Application.Validation;

using FluentValidation;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

namespace DistributedFlowLab.Application;

/// <summary>Registers the Application layer (MediatR handlers, validators, pipeline).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}