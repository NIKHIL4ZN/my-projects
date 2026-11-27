using SmartX.Core.Topology.DataStructures;
using SmartX.Core.Topology.Models;

namespace SmartX.Core.Topology.Algorithms;

public static class MeshTraversal
{
    public static MeshTraversalResult BreadthFirstSearch(
        MeshGraph graph,
        Guid startNodeId)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var startNode = graph.GetNode(startNodeId);

        var parents = new Dictionary<Guid, Guid?>();
        var hopCounts = new Dictionary<Guid, int>();
        var pending = new Queue<Guid>();

        if (startNode.IsOnline)
        {
            parents.Add(startNodeId, null);
            hopCounts.Add(startNodeId, 0);
            pending.Enqueue(startNodeId);
        }

        while (pending.TryDequeue(out var currentNodeId))
        {
            foreach (var connection in
                     graph.GetAvailableConnections(currentNodeId))
            {
                var neighbourId = connection.SecondNodeId;

                // Recording a parent also marks the node visited,
                // preventing cycles and repeated queue entries.
                if (!parents.TryAdd(neighbourId, currentNodeId))
                {
                    continue;
                }

                hopCounts.Add(
                    neighbourId,
                    hopCounts[currentNodeId] + 1);

                pending.Enqueue(neighbourId);
            }
        }

        var unreachableNodeIds = graph.GetNodes()
            .Where(node => !parents.ContainsKey(node.Id))
            .Select(node => node.Id);

        return new MeshTraversalResult(
            startNodeId,
            parents,
            hopCounts,
            unreachableNodeIds);
    }
}

// Code attributes

/*
Title: Breadth First Search or BFS for a Graph
Author(s): GeeksforGeeks
Date: 2026
Version: Not applicable
Type: Online tutorial
Availability: https://www.geeksforgeeks.org/dsa/breadth-first-search-or-bfs-for-a-graph/
*/