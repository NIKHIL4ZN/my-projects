using SmartX.Core.Topology.DTOs;
using SmartX.Core.Topology.Models;

namespace SmartX.Core.Topology.Interfaces;

public interface ITopologyService
{
    Task<TopologyResponse> GetTopologyAsync(
        CancellationToken cancellationToken = default);

    Task<TopologyConnectivityResponse> GetConnectivityAsync(
        Guid gatewayId,
        Guid? offlineNodeId = null,
        CancellationToken cancellationToken = default);

    Task<MeshSpanningTreeResult> GetSpanningTreeAsync(
        Guid gatewayId,
        Guid? offlineNodeId = null,
        CancellationToken cancellationToken = default);

    Task<TopologyRouteResponse> GetRouteAsync(
        Guid gatewayId,
        string macAddress,
        bool useSpanningTree = false,
        Guid? offlineNodeId = null,
        CancellationToken cancellationToken = default);
}