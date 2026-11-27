using SmartX.Core.Topology.Models;

namespace SmartX.Core.Topology.DTOs;

public sealed record TopologyNodeResponse(
    Guid Id,
    Guid? SensorId,
    string Name,
    string MacAddress,
    string DeploymentLocation,
    bool IsGateway,
    bool IsOnline);

public sealed record TopologyConnectionResponse(
    Guid Id,
    Guid FirstNodeId,
    Guid SecondNodeId,
    double EnergyCost,
    bool IsEnabled);

public sealed record TopologyResponse(
    IReadOnlyList<TopologyNodeResponse> Nodes,
    IReadOnlyList<TopologyConnectionResponse> Connections);

public sealed record TopologyConnectivityResponse(
    Guid GatewayId,
    IReadOnlyList<Guid> ReachableNodeIds,
    IReadOnlyList<Guid> UnreachableNodeIds,
    IReadOnlyDictionary<Guid, int> HopCounts);

public sealed record TopologyRouteResponse(
    Guid GatewayId,
    Guid NodeId,
    string RouteMode,
    bool IsReachable,
    int? HopCount,
    IReadOnlyList<MeshNode> Path);