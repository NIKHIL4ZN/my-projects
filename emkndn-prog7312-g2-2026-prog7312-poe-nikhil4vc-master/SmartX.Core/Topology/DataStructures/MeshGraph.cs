using SmartX.Core.Topology.Models;

namespace SmartX.Core.Topology.DataStructures;

/// <summary>
/// An undirected weighted graph backed by adjacency dictionaries.
/// Construct a separate graph instance for each network snapshot.
/// </summary>
public sealed class MeshGraph
{
    private readonly Dictionary<Guid, MeshNode> _nodes = new();

    private readonly Dictionary<Guid, Dictionary<Guid, double>>
        _adjacency = new();

    private readonly Dictionary<string, Guid> _nodeIdsByMac =
        new(StringComparer.OrdinalIgnoreCase);

    public int NodeCount => _nodes.Count;

    public int ConnectionCount =>
        _adjacency.Values.Sum(neighbours => neighbours.Count) / 2;

    public void AddNode(MeshNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (node.Id == Guid.Empty)
        {
            throw new ArgumentException(
                "A mesh node must have a valid ID.");
        }

        if (string.IsNullOrWhiteSpace(node.Name) ||
            string.IsNullOrWhiteSpace(node.MacAddress))
        {
            throw new ArgumentException(
                "A mesh node requires a name and MAC address.");
        }

        var normalisedNode = node with
        {
            Name = node.Name.Trim(),
            MacAddress = node.MacAddress.Trim(),
            DeploymentLocation = node.DeploymentLocation.Trim()
        };

        if (_nodes.ContainsKey(normalisedNode.Id))
        {
            throw new ArgumentException(
                "A node with this ID already exists.");
        }

        if (_nodeIdsByMac.ContainsKey(normalisedNode.MacAddress))
        {
            throw new ArgumentException(
                "A node with this MAC address already exists.");
        }

        _nodes.Add(normalisedNode.Id, normalisedNode);
        _nodeIdsByMac.Add(
            normalisedNode.MacAddress,
            normalisedNode.Id);

        _adjacency.Add(
            normalisedNode.Id,
            new Dictionary<Guid, double>());
    }

    public void AddConnection(
        Guid firstNodeId,
        Guid secondNodeId,
        double energyCost)
    {
        GetNode(firstNodeId);
        GetNode(secondNodeId);

        if (firstNodeId == secondNodeId)
        {
            throw new ArgumentException(
                "A node cannot connect to itself.");
        }

        if (!double.IsFinite(energyCost) || energyCost <= 0)
        {
            throw new ArgumentException(
                "Connection energy cost must be finite and positive.");
        }

        if (_adjacency[firstNodeId].ContainsKey(secondNodeId))
        {
            throw new ArgumentException(
                "This connection already exists.");
        }

        // Store both directions because wireless connections
        // are modelled as undirected in this simulation.
        _adjacency[firstNodeId].Add(secondNodeId, energyCost);
        _adjacency[secondNodeId].Add(firstNodeId, energyCost);
    }

    public MeshNode GetNode(Guid nodeId)
    {
        if (!_nodes.TryGetValue(nodeId, out var node))
        {
            throw new KeyNotFoundException(
                $"Mesh node '{nodeId}' was not found.");
        }

        return node;
    }

    public MeshNode? FindByMacAddress(string macAddress)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
        {
            return null;
        }

        return _nodeIdsByMac.TryGetValue(
            macAddress.Trim(),
            out var nodeId)
            ? _nodes[nodeId]
            : null;
    }

    public IReadOnlyList<MeshNode> GetNodes()
    {
        return _nodes.Values
            .OrderBy(node => node.Name)
            .ThenBy(node => node.Id)
            .ToList();
    }

    public IReadOnlyList<MeshConnection> GetConnections()
    {
        var results = new List<MeshConnection>();

        foreach (var entry in _adjacency)
        {
            foreach (var neighbour in entry.Value)
            {
                // Each undirected connection is returned once.
                if (entry.Key.CompareTo(neighbour.Key) < 0)
                {
                    results.Add(new MeshConnection(
                        entry.Key,
                        neighbour.Key,
                        neighbour.Value));
                }
            }
        }

        return results
            .OrderBy(connection => connection.FirstNodeId)
            .ThenBy(connection => connection.SecondNodeId)
            .ToList();
    }

    public IReadOnlyList<MeshConnection> GetAvailableConnections(
        Guid nodeId)
    {
        var node = GetNode(nodeId);

        if (!node.IsOnline)
        {
            return Array.Empty<MeshConnection>();
        }

        return _adjacency[nodeId]
            .Where(neighbour => _nodes[neighbour.Key].IsOnline)
            .OrderBy(neighbour => neighbour.Key)
            .Select(neighbour => new MeshConnection(
                nodeId,
                neighbour.Key,
                neighbour.Value))
            .ToList();
    }

    public void SetNodeOnline(Guid nodeId, bool isOnline)
    {
        var node = GetNode(nodeId);

        _nodes[nodeId] = node with
        {
            IsOnline = isOnline
        };
    }

    public bool RemoveConnection(
        Guid firstNodeId,
        Guid secondNodeId)
    {
        GetNode(firstNodeId);
        GetNode(secondNodeId);

        var removed =
            _adjacency[firstNodeId].Remove(secondNodeId);

        _adjacency[secondNodeId].Remove(firstNodeId);

        return removed;
    }
}

// Code attributes

/*
Title: C Program to Implement Adjacency List
Author(s): GeeksforGeeks
Date: 2025
Version: Not applicable
Type: Online tutorial
Availability: https://www.geeksforgeeks.org/c/c-program-to-implement-adjacency-list/
*/