using DistributedFlowLab.Application.Abstractions;
using DistributedFlowLab.Infrastructure.Engine;
using DistributedFlowLab.Infrastructure.Events;
using DistributedFlowLab.Infrastructure.Persistence;
using DistributedFlowLab.Infrastructure.Realtime;

using Microsoft.Extensions.DependencyInjection;

namespace DistributedFlowLab.Infrastructure;

/// <summary>
/// Registers the Infrastructure adapters behind the Application ports:
/// in-memory persistence (EF Core in V1), the sequenced event pipeline
/// (SignalR publisher in Sprint 2), and the simulation engine (ADR-007).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Persistence (MVP in-memory; singletons so all consumers share state).
        services.AddSingleton<IScenarioRepository, InMemoryScenarioRepository>();
        services.AddSingleton<ISimulationRepository, InMemorySimulationRepository>();
        services.AddSingleton<IEventStore, InMemoryEventStore>();

        // Event pipeline (ADR-009: single emitter mints every sequence;
        // ADR-002: SignalR dispatches to per-simulation groups).
        services.AddSingleton<SignalREventPublisher>();
        services.AddSingleton<IEventPublisher>(sp => sp.GetRequiredService<SignalREventPublisher>());
        services.AddSingleton<ISimulationStatePublisher>(sp => sp.GetRequiredService<SignalREventPublisher>());
        services.AddSingleton<IEventEmitter, SequencedEventEmitter>();

        // Simulation engine (ADR-007).
        services.AddSingleton<ISimulationClock, DelaySimulationClock>();
        services.AddSingleton<ChannelSimulationScheduler>();
        services.AddSingleton<ISimulationScheduler>(sp => sp.GetRequiredService<ChannelSimulationScheduler>());
        services.AddSingleton<SimulationRunner>();
        services.AddHostedService<SimulationEngineHostedService>();

        return services;
    }
}