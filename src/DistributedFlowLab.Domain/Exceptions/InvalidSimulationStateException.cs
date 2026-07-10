using DistributedFlowLab.Domain.Enums;

namespace DistributedFlowLab.Domain.Exceptions;

/// <summary>
/// Thrown when a lifecycle command targets a <see cref="Entities.Simulation"/>
/// whose current status does not permit the transition (e.g. resume when not
/// paused). Surfaced by the API as HTTP 409 Conflict (RFC 7807).
/// </summary>
public sealed class InvalidSimulationStateException : DomainException
{
    public InvalidSimulationStateException(Guid simulationId, SimulationStatus current, string attempted)
        : base($"Simulation '{simulationId}' cannot '{attempted}' while in status '{current}'.")
    {
        SimulationId = simulationId;
        CurrentStatus = current;
        AttemptedTransition = attempted;
    }

    public Guid SimulationId { get; }

    public SimulationStatus CurrentStatus { get; }

    public string AttemptedTransition { get; }
}