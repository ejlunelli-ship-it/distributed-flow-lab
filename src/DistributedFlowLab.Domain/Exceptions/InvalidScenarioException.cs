namespace DistributedFlowLab.Domain.Exceptions;

/// <summary>
/// Thrown when a <see cref="Entities.Scenario"/> topology violates a domain
/// invariant (duplicate node ids, edges referencing unknown nodes, …).
/// </summary>
public sealed class InvalidScenarioException : DomainException
{
    public InvalidScenarioException(string message)
        : base(message)
    {
    }
}