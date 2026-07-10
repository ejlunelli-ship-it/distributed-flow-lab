using System.Collections.Concurrent;

using DistributedFlowLab.Infrastructure.Engine;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DistributedFlowLab.Infrastructure.Engine;

/// <summary>
/// The engine host (ADR-007): a <see cref="BackgroundService"/> that consumes
/// scheduled simulation ids from <see cref="ChannelSimulationScheduler"/> and
/// runs each simulation's tick loop as an independent tracked task, off the
/// request thread. Graceful shutdown cancels all active runs.
/// </summary>
public sealed partial class SimulationEngineHostedService : BackgroundService
{
    private readonly ChannelSimulationScheduler _scheduler;
    private readonly SimulationRunner _runner;
    private readonly ILogger<SimulationEngineHostedService> _logger;
    private readonly ConcurrentDictionary<Guid, Task> _activeRuns = new();

    public SimulationEngineHostedService(
        ChannelSimulationScheduler scheduler,
        SimulationRunner runner,
        ILogger<SimulationEngineHostedService> logger)
    {
        _scheduler = scheduler;
        _runner = runner;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var simulationId in _scheduler.Reader.ReadAllAsync(stoppingToken))
        {
            if (_activeRuns.ContainsKey(simulationId))
            {
                LogDuplicateSchedule(simulationId);
                continue;
            }

            LogRunStarting(simulationId);
            var run = Task.Run(() => _runner.RunAsync(simulationId, stoppingToken), stoppingToken);
            _activeRuns[simulationId] = run;
            _ = run.ContinueWith(
                _ => _activeRuns.TryRemove(simulationId, out Task? _),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Simulation {SimulationId} is already running; ignoring duplicate schedule.")]
    private partial void LogDuplicateSchedule(Guid simulationId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting simulation run {SimulationId}.")]
    private partial void LogRunStarting(Guid simulationId);

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await Task.WhenAll(_activeRuns.Values).WaitAsync(TimeSpan.FromSeconds(5), cancellationToken)
            .ContinueWith(_ => { }, TaskScheduler.Default);
    }
}