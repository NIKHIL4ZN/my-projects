namespace SmartX.Core.Topology.Models;

/// <summary>
/// An undirected wireless connection.
/// EnergyCost represents a simulated transmission cost.
/// </summary>
public sealed record MeshConnection(
    Guid FirstNodeId,
    Guid SecondNodeId,
    double EnergyCost);