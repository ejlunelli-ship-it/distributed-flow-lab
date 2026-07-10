using System.Threading.Channels;

using DistributedFlowLab.Application.Abstractions;

namespace DistributedFlowLab.Infrastructure.Engine;

/// <summary>
/// Hands started simulations to the engine loop through an unbounded channel:
/// the start command writes, <see cref="SimulationEngineHostedService"/> reads.
/// Registered as a singleton so both sides share the same channel.
/// </summary>
public sealed class ChannelSimulationScheduler : ISimulationScheduler
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    public ChannelReader<Guid> Reader => _channel.Reader;

    public ValueTask ScheduleAsync(Guid simulationId, CancellationToken cancellationToken)
        => _channel.Writer.WriteAsync(simulationId, cancellationToken);
}