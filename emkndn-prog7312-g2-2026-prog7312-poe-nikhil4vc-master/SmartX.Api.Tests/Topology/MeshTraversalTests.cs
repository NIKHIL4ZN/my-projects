using SmartX.Core.Topology.Algorithms;
using SmartX.Core.Topology.DataStructures;
using SmartX.Core.Topology.Models;
using Xunit;

namespace SmartX.Api.Tests.Topology;

public class MeshTraversalTests
{
    [Fact]
    public void BreadthFirstSearch_FindsFewestHopPathInCyclicGraph()
    {
        var graph = new MeshGraph();

        var gateway = AddNode(graph, 1, true);
        var first = AddNode(graph, 2);
        var second = AddNode(graph, 3);
        var target = AddNode(graph, 4);

        graph.AddConnection(gateway.Id, first.Id, 1);
        graph.AddConnection(first.Id, second.Id, 1);
        graph.AddConnection(second.Id, target.Id, 1);
        graph.AddConnection(gateway.Id, target.Id, 10);

        var result = MeshTraversal.BreadthFirstSearch(
            graph, gateway.Id);

        Assert.Equal(4, result.ReachableNodeIds.Count);
        Assert.Empty(result.UnreachableNodeIds);
        Assert.Equal(1, result.GetHopCount(target.Id));

        Assert.Equal(
            new[] { gateway.Id, target.Id },
            result.GetPathTo(target.Id));
    }

    [Fact]
    public void BreadthFirstSearch_ReportsDisconnectedDevices()
    {
        var graph = new MeshGraph();

        var gateway = AddNode(graph, 1, true);
        var connected = AddNode(graph, 2);
        var isolated = AddNode(graph, 3);

        graph.AddConnection(gateway.Id, connected.Id, 1);

        var result = MeshTraversal.BreadthFirstSearch(
            graph, gateway.Id);

        Assert.Contains(isolated.Id, result.UnreachableNodeIds);
        Assert.Null(result.GetHopCount(isolated.Id));
        Assert.Empty(result.GetPathTo(isolated.Id));
        Assert.Equal(1, result.GetHopCount(connected.Id));
    }

    [Fact]
    public void BreadthFirstSearch_RoutesAroundFailedNode()
    {
        var graph = new MeshGraph();

        var gateway = AddNode(graph, 1, true);
        var failed = AddNode(graph, 2);
        var alternate = AddNode(graph, 3);
        var target = AddNode(graph, 4);

        graph.AddConnection(gateway.Id, failed.Id, 1);
        graph.AddConnection(failed.Id, target.Id, 1);
        graph.AddConnection(gateway.Id, alternate.Id, 2);
        graph.AddConnection(alternate.Id, target.Id, 2);

        graph.SetNodeOnline(failed.Id, false);

        var result = MeshTraversal.BreadthFirstSearch(
            graph, gateway.Id);

        Assert.Contains(failed.Id, result.UnreachableNodeIds);

        Assert.Equal(
            new[] { gateway.Id, alternate.Id, target.Id },
            result.GetPathTo(target.Id));

        Assert.Equal(2, result.GetHopCount(target.Id));
    }

    [Fact]
    public void BreadthFirstSearch_ReportsAllNodesWhenGatewayIsOffline()
    {
        var graph = new MeshGraph();

        var gateway = AddNode(graph, 1, true);
        var sensor = AddNode(graph, 2);

        graph.AddConnection(gateway.Id, sensor.Id, 1);
        graph.SetNodeOnline(gateway.Id, false);

        var result = MeshTraversal.BreadthFirstSearch(
            graph, gateway.Id);

        Assert.Empty(result.ReachableNodeIds);
        Assert.Equal(2, result.UnreachableNodeIds.Count);
        Assert.Empty(result.GetPathTo(sensor.Id));
    }

    [Fact]
    public void BreadthFirstSearch_ReturnsStartNodeWithZeroHops()
    {
        var graph = new MeshGraph();
        var gateway = AddNode(graph, 1, true);

        var result = MeshTraversal.BreadthFirstSearch(
            graph, gateway.Id);

        Assert.Equal(0, result.GetHopCount(gateway.Id));
        Assert.Equal(
            new[] { gateway.Id },
            result.GetPathTo(gateway.Id));
    }

    [Fact]
    public void BreadthFirstSearch_RejectsUnknownStartNode()
    {
        var graph = new MeshGraph();

        Assert.Throws<KeyNotFoundException>(() =>
            MeshTraversal.BreadthFirstSearch(
                graph, Guid.NewGuid()));
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