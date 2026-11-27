using SmartX.Core.Topology.Algorithms;
using SmartX.Core.Topology.DataStructures;
using SmartX.Core.Topology.Models;
using Xunit;

namespace SmartX.Api.Tests.Topology;

public class MeshMinimumSpanningTreeTests
{
    [Fact]
    public void Calculate_SelectsMinimumTotalCostWithoutCycles()
    {
        var graph = new MeshGraph();

        var gateway = AddNode(graph, 1, true);
        var first = AddNode(graph, 2);
        var second = AddNode(graph, 3);
        var third = AddNode(graph, 4);

        graph.AddConnection(gateway.Id, first.Id, 1);
        graph.AddConnection(gateway.Id, second.Id, 4);
        graph.AddConnection(first.Id, second.Id, 2);
        graph.AddConnection(first.Id, third.Id, 5);
        graph.AddConnection(second.Id, third.Id, 3);

        var result = MeshMinimumSpanningTree.Calculate(
            graph, gateway.Id);

        Assert.True(result.SpansEntireNetwork);
        Assert.Equal(4, result.ConnectedNodeIds.Count);
        Assert.Equal(3, result.Connections.Count);
        Assert.Equal(6.0, result.TotalEnergyCost);

        Assert.Equal(
            new[] { 1.0, 2.0, 3.0 },
            result.Connections
                .Select(connection => connection.EnergyCost)
                .OrderBy(cost => cost));

        // The selected edges must actually connect every node.
        var treeGraph = new MeshGraph();

        foreach (var node in graph.GetNodes())
        {
            treeGraph.AddNode(node);
        }

        foreach (var connection in result.Connections)
        {
            treeGraph.AddConnection(
                connection.FirstNodeId,
                connection.SecondNodeId,
                connection.EnergyCost);
        }

        var traversal = MeshTraversal.BreadthFirstSearch(
            treeGraph, gateway.Id);

        Assert.Empty(traversal.UnreachableNodeIds);
    }

    [Fact]
    public void Calculate_ReportsNodesOutsideGatewayComponent()
    {
        var graph = new MeshGraph();

        var gateway = AddNode(graph, 1, true);
        var connected = AddNode(graph, 2);
        var isolated = AddNode(graph, 3);
        var isolatedNeighbour = AddNode(graph, 4);

        graph.AddConnection(gateway.Id, connected.Id, 2);
        graph.AddConnection(isolated.Id, isolatedNeighbour.Id, 1);

        var result = MeshMinimumSpanningTree.Calculate(
            graph, gateway.Id);

        Assert.False(result.SpansEntireNetwork);
        Assert.Equal(2, result.ConnectedNodeIds.Count);
        Assert.Single(result.Connections);
        Assert.Equal(2.0, result.TotalEnergyCost);

        Assert.Contains(isolated.Id, result.UnreachableNodeIds);
        Assert.Contains(
            isolatedNeighbour.Id,
            result.UnreachableNodeIds);
    }

    [Fact]
    public void Calculate_RecalculatesAroundFailedNode()
    {
        var graph = new MeshGraph();

        var gateway = AddNode(graph, 1, true);
        var relay = AddNode(graph, 2);
        var target = AddNode(graph, 3);

        graph.AddConnection(gateway.Id, relay.Id, 1);
        graph.AddConnection(relay.Id, target.Id, 1);
        graph.AddConnection(gateway.Id, target.Id, 8);

        var beforeFailure = MeshMinimumSpanningTree.Calculate(
            graph, gateway.Id);

        Assert.Equal(2.0, beforeFailure.TotalEnergyCost);

        graph.SetNodeOnline(relay.Id, false);

        var afterFailure = MeshMinimumSpanningTree.Calculate(
            graph, gateway.Id);

        var connection = Assert.Single(afterFailure.Connections);

        Assert.Equal(gateway.Id, connection.FirstNodeId);
        Assert.Equal(target.Id, connection.SecondNodeId);
        Assert.Equal(8.0, afterFailure.TotalEnergyCost);
        Assert.Contains(relay.Id, afterFailure.UnreachableNodeIds);
        Assert.Contains(target.Id, afterFailure.ConnectedNodeIds);
    }

    [Fact]
    public void Calculate_OfflineGatewayCannotReachAnyNodes()
    {
        var graph = new MeshGraph();

        var gateway = AddNode(graph, 1, true);
        var sensor = AddNode(graph, 2);

        graph.AddConnection(gateway.Id, sensor.Id, 1);
        graph.SetNodeOnline(gateway.Id, false);

        var result = MeshMinimumSpanningTree.Calculate(
            graph, gateway.Id);

        Assert.Empty(result.Connections);
        Assert.Empty(result.ConnectedNodeIds);
        Assert.Equal(2, result.UnreachableNodeIds.Count);
        Assert.False(result.SpansEntireNetwork);
        Assert.Equal(0.0, result.TotalEnergyCost);
    }

    [Fact]
    public void Calculate_SingleGatewayNeedsNoConnections()
    {
        var graph = new MeshGraph();
        var gateway = AddNode(graph, 1, true);

        var result = MeshMinimumSpanningTree.Calculate(
            graph, gateway.Id);

        Assert.True(result.SpansEntireNetwork);
        Assert.Single(result.ConnectedNodeIds);
        Assert.Empty(result.Connections);
        Assert.Equal(0.0, result.TotalEnergyCost);
    }

    [Fact]
    public void Calculate_HandlesEqualCostConnectionsConsistently()
    {
        var graph = new MeshGraph();

        var gateway = AddNode(graph, 1, true);
        var first = AddNode(graph, 2);
        var second = AddNode(graph, 3);

        graph.AddConnection(gateway.Id, first.Id, 1);
        graph.AddConnection(gateway.Id, second.Id, 1);
        graph.AddConnection(first.Id, second.Id, 1);

        var firstResult = MeshMinimumSpanningTree.Calculate(
            graph, gateway.Id);

        var secondResult = MeshMinimumSpanningTree.Calculate(
            graph, gateway.Id);

        Assert.True(firstResult.SpansEntireNetwork);
        Assert.Equal(2, firstResult.Connections.Count);
        Assert.Equal(2.0, firstResult.TotalEnergyCost);

        Assert.Equal(
            firstResult.Connections.ToArray(),
            secondResult.Connections.ToArray());
    }

    [Fact]
    public void Calculate_RejectsUnknownGateway()
    {
        var graph = new MeshGraph();

        Assert.Throws<KeyNotFoundException>(() =>
            MeshMinimumSpanningTree.Calculate(
                graph, Guid.NewGuid()));
    }

    [Fact]
    public void Calculate_RejectsSensorAsGateway()
    {
        var graph = new MeshGraph();
        var sensor = AddNode(graph, 1);

        Assert.Throws<ArgumentException>(() =>
            MeshMinimumSpanningTree.Calculate(
                graph, sensor.Id));
    }

    private static MeshNode AddNode(
        MeshGraph graph,
        int number,
        bool isGateway = false)
    {
        var node = new MeshNode(
            Guid.NewGuid(),
            $"Node {number}",
            $"AA:00:00:00:00:{number:X2}",
            "Test Facility",
            isGateway,
            true);

        graph.AddNode(node);

        return node;
    }
}