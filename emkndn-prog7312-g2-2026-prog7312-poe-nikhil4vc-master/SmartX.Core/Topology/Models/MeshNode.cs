namespace SmartX.Core.Topology.Models;

public sealed record MeshNode(
    Guid Id,
    string Name,
    string MacAddress,
    string DeploymentLocation,
    bool IsGateway,
    bool IsOnline);