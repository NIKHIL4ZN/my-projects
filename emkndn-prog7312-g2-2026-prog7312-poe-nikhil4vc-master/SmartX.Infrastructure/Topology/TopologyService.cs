using Microsoft.EntityFrameworkCore;
using SmartX.Core.Topology.Algorithms;
using SmartX.Core.Topology.DataStructures;
using SmartX.Core.Topology.DTOs;
using SmartX.Core.Topology.Interfaces;
using SmartX.Core.Topology.Models;
using SmartX.Infrastructure.Data;

namespace SmartX.Infrastructure.Topology;

public sealed class TopologyService : ITopologyService
{
    private readonly SmartXDbContext _dbContext;

    public TopologyService(SmartXDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TopologyResponse> GetTopologyAsync(
        CancellationToken cancellationToken = default)
    {
        var nodes = await _dbContext.MeshNodes
            .AsNoTracking()
            .OrderBy(node => node.Name)
            .Select(node => new TopologyNodeResponse(
                node.Id,
                node.SensorId,
                node.Name,
                node.MacAddress,
                node.DeploymentLocation,
                node.IsGateway,
                node.IsOnline))
            .ToListAsync(cancellationToken);

        var connections = await _dbContext.MeshConnections
            .AsNoTracking()
            .OrderBy(connection => connection.Id)
            .Select(connection => new TopologyConnectionResponse(
                connection.Id,
                connection.FirstNodeId,
                connection.SecondNodeId,
                connection.EnergyCost,
                connection.IsEnabled))
            .ToListAsync(cancellationToken);

        return new TopologyResponse(nodes, connections);
    }

    public async Task<TopologyConnectivityResponse>
        GetConnectivityAsync(
            Guid gatewayId,
            Guid? offlineNodeId = null,
            CancellationToken cancellationToken = default)
    {
        var graph = await LoadGraphAsync(
            offlineNodeId,
            cancellationToken);

        ValidateGateway(graph, gatewayId);

        var traversal = MeshTraversal.BreadthFirstSearch(
            graph,
            gatewayId);

        var hopCounts = traversal.ReachableNodeIds
            .ToDictionary(
                nodeId => nodeId,
                nodeId => traversal.GetHopCount(nodeId)!.Value);

        return new TopologyConnectivityResponse(
            gatewayId,
            traversal.ReachableNodeIds,
            traversal.UnreachableNodeIds,
            hopCounts);
    }

    public async Task<MeshSpanningTreeResult>
        GetSpanningTreeAsync(
            Guid gatewayId,
            Guid? offlineNodeId = null,
            CancellationToken cancellationToken = default)
    {
        var graph = await LoadGraphAsync(
            offlineNodeId,
            cancellationToken);

        ValidateGateway(graph, gatewayId);

        return MeshMinimumSpanningTree.Calculate(
            graph,
            gatewayId);
    }

    public async Task<TopologyRouteResponse> GetRouteAsync(
        Guid gatewayId,
        string macAddress,
        bool useSpanningTree = false,
        Guid? offlineNodeId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
        {
            throw new ArgumentException(
                "A device MAC address is required.");
        }

        var graph = await LoadGraphAsync(
            offlineNodeId,
            cancellationToken);

        ValidateGateway(graph, gatewayId);

        var target = graph.FindByMacAddress(macAddress)
            ?? throw new KeyNotFoundException(
                $"Mesh device '{macAddress}' was not found.");

        var routingGraph = graph;

        if (useSpanningTree)
        {
            var spanningTree = MeshMinimumSpanningTree.Calculate(
                graph,
                gatewayId);

            routingGraph = new MeshGraph();

            foreach (var node in graph.GetNodes())
            {
                routingGraph.AddNode(node);
            }

            foreach (var connection in spanningTree.Connections)
            {
                routingGraph.AddConnection(
                    connection.FirstNodeId,
                    connection.SecondNodeId,
                    connection.EnergyCost);
            }
        }

        var traversal = MeshTraversal.BreadthFirstSearch(
            routingGraph,
            gatewayId);

        // BFS records gateway-to-device paths.
        // Reverse this for telemetry travelling to the gateway.
        var path = traversal.GetPathTo(target.Id)
            .Reverse()
            .Select(nodeId => graph.GetNode(nodeId))
            .ToList();

        return new TopologyRouteResponse(
            gatewayId,
            target.Id,
            useSpanningTree ? "MinimumSpanningTree" : "FewestHops",
            path.Count > 0,
            traversal.GetHopCount(target.Id),
            path);
    }

    private async Task<MeshGraph> LoadGraphAsync(
        Guid? offlineNodeId,
        CancellationToken cancellationToken)
    {
        var topology = await GetTopologyAsync(cancellationToken);
        var graph = new MeshGraph();

        foreach (var node in topology.Nodes)
        {
            graph.AddNode(new MeshNode(
                node.Id,
                node.Name,
                node.MacAddress,
                node.DeploymentLocation,
                node.IsGateway,
                node.IsOnline));
        }

        foreach (var connection in topology.Connections)
        {
            if (!connection.IsEnabled)
            {
                continue;
            }

            graph.AddConnection(
                connection.FirstNodeId,
                connection.SecondNodeId,
                connection.EnergyCost);
        }

        if (offlineNodeId.HasValue)
        {
            // Only this request's graph changes.
            // No database or sensor status is modified.
            graph.SetNodeOnline(offlineNodeId.Value, false);
        }

        return graph;
    }

    private static void ValidateGateway(
        MeshGraph graph,
        Guid gatewayId)
    {
        if (!graph.GetNode(gatewayId).IsGateway)
        {
            throw new ArgumentException(
                "The selected node is not a gateway.");
        }
    }
}