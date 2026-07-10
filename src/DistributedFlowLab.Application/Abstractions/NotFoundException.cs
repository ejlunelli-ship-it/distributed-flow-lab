namespace DistributedFlowLab.Application.Abstractions;

/// <summary>
/// Thrown by handlers when a referenced resource does not exist.
/// Surfaced by the API as HTTP 404 (RFC 7807).
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string resource, Guid id)
        : base($"{resource} '{id}' was not found.")
    {
    }
}