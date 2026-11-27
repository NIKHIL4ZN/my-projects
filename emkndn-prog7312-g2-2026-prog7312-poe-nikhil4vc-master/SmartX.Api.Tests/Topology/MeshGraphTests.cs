using SmartX.Core.Topology.DataStructures;
using SmartX.Core.Topology.Models;
using Xunit;

namespace SmartX.Api.Tests.Topology;

public class MeshGraphTests
{
    [Fact]
    public void AddConnection_CreatesBothDirectionsAndCountsOnce()
    {
        var graph = new MeshGraph();
        var first = CreateNode("Sensor A", "AA:00:00:00:00:01");
        var second = CreateNode("Gateway", "AA:00:00:00:00:02", true);

        graph.AddNode(first);
        graph.AddNode(second);
        graph.AddConnection(first.Id, second.Id, 2.5);

        Assert.Equal(2, graph.NodeCount);
        Assert.Equal(1, graph.ConnectionCount);
        Assert.Single(graph.GetConnections());

        var forward = Assert.Single(
            graph.GetAvailableConnections(first.Id));

        var reverse = Assert.Single(
            graph.GetAvailableConnections(second.Id));

        Assert.Equal(second.Id, forward.SecondNodeId);
        Assert.Equal(first.Id, reverse.SecondNodeId);
        Assert.Equal(2.5, forward.EnergyCost);
        Assert.Equal(2.5, reverse.EnergyCost);
    }

    [Fact]
    public void FindByMacAddress_IgnoresCaseAndSurroundingSpaces()
    {
        var graph = new MeshGraph();
        var node = CreateNode("Sensor A", "AA:BB:CC:00:00:01");

        graph.AddNode(node);

        Assert.Equal(
            node.Id,
            graph.FindByMacAddress(" aa:bb:cc:00:00:01 ")?.Id);

        Assert.Null(graph.FindByMacAddress("UNKNOWN"));
    }

    [Fact]
    public void AddNode_RejectsDuplicateIdsAndMacAddresses()
    {
        var graph = new MeshGraph();
        var node = CreateNode("Sensor A", "AA:BB:CC:00:00:01");

        graph.AddNode(node);

        Assert.Throws<ArgumentException>(() =>
            graph.AddNode(node with
            {
                MacAddress = "AA:BB:CC:00:00:02"
            }));

        Assert.Throws<ArgumentException>(() =>
            graph.AddNode(node with
            {
                Id = Guid.NewGuid(),
                MacAddress = "aa:bb:cc:00:00:01"
            }));
    }

    [Fact]
    public void AddConnection_RejectsSelfConnectionsAndUnknownNodes()
    {
        var graph = new MeshGraph();
        var node = CreateNode("Sensor A", "AA:00:00:00:00:01");

        graph.AddNode(node);

        Assert.Throws<ArgumentException>(() =>
            graph.AddConnection(node.Id, node.Id, 1));

        Assert.Throws<KeyNotFoundException>(() =>
            graph.AddConnection(node.Id, Guid.NewGuid(), 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void AddConnection_RejectsInvalidEnergyCosts(double cost)
    {
        var graph = new MeshGraph();
        var first = CreateNode("A", "AA:00:00:00:00:01");
        var second = CreateNode("B", "AA:00:00:00:00:02");

        graph.AddNode(first);
        graph.AddNode(second);

        Assert.Throws<ArgumentException>(() =>
            graph.AddConnection(first.Id, second.Id, cost));

        Assert.Equal(0, graph.ConnectionCount);
    }

    [Fact]
    public void AddConnection_RejectsReverseDuplicate()
    {
        var graph = new MeshGraph();
        var first = CreateNode("A", "AA:00:00:00:00:01");
        var second = CreateNode("B", "AA:00:00:00:00:02");

        graph.AddNode(first);
        graph.AddNode(second);
        graph.AddConnection(first.Id, second.Id, 1);

        Assert.Throws<ArgumentException>(() =>
            graph.AddConnection(second.Id, first.Id, 2));

        Assert.Equal(1, graph.ConnectionCount);
    }

    [Fact]
    public void SetNodeOnline_ExcludesFailedNodesAndSupportsRecovery()
    {
        var graph = new MeshGraph();
        var first = CreateNode("A", "AA:00:00:00:00:01");
        var second = CreateNode("B", "AA:00:00:00:00:02");

        graph.AddNode(first);
        graph.AddNode(second);
        graph.AddConnection(first.Id, second.Id, 1);

        graph.SetNodeOnline(second.Id, false);

        Assert.Empty(graph.GetAvailableConnections(first.Id));
        Assert.Empty(graph.GetAvailableConnections(second.Id));
        Assert.Equal(1, graph.ConnectionCount);

        graph.SetNodeOnline(second.Id, true);

        Assert.Single(graph.GetAvailableConnections(first.Id));
    }

    [Fact]
    public void RemoveConnection_RemovesBothDirections()
    {
        var graph = new MeshGraph();
        var first = CreateNode("A", "AA:00:00:00:00:01");
        var second = CreateNode("B", "AA:00:00:00:00:02");

        graph.AddNode(first);
        graph.AddNode(second);
        graph.AddConnection(first.Id, second.Id, 1);

        Assert.True(graph.RemoveConnection(first.Id, second.Id));
        Assert.False(graph.RemoveConnection(first.Id, second.Id));

        Assert.Empty(graph.GetAvailableConnections(first.Id));
        Assert.Empty(graph.GetAvailableConnections(second.Id));
        Assert.Equal(0, graph.ConnectionCount);
    }

    private static MeshNode CreateNode(
        string name,
        string macAddress,
        bool isGateway = false)
    {
        return new MeshNode(
            Guid.NewGuid(),
            name,
            macAddress,
            "Test Facility",
            isGateway,
            true);
    }
}