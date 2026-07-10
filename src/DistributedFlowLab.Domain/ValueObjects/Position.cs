namespace DistributedFlowLab.Domain.ValueObjects;

/// <summary>Canvas coordinates of a <see cref="Entities.Node"/>.</summary>
public readonly record struct Position(int X, int Y);