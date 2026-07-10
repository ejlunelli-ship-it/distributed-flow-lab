namespace DistributedFlowLab.Domain.Exceptions;

/// <summary>Base type for all domain-invariant violations.</summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message)
        : base(message)
    {
    }
}