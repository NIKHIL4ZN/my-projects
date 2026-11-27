using SmartX.Core.Topology.DataStructures;
using SmartX.Core.Topology.Models;

namespace SmartX.Core.Topology.Algorithms;

public static class MeshMinimumSpanningTree
{
    public static MeshSpanningTreeResult Calculate(
        MeshGraph graph,
        Guid gatewayId)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var gateway = graph.GetNode(gatewayId);

        if (!gateway.IsGateway)
        {
            throw new ArgumentException(
                "The starting node must be a gateway.");
        }

        var visited = new HashSet<Guid>();
        var selectedConnections = new List<MeshConnection>();

        // Cost comes first. Node IDs provide deterministic
        // tie-breaking when connections have equal costs.
        var candidates = new PriorityQueue<
            MeshConnection,
            (double Cost, Guid FirstNodeId, Guid SecondNodeId)>();

        if (gateway.IsOnline)
        {
            visited.Add(gatewayId);
            AddCandidates(gatewayId);
        }

        while (candidates.TryDequeue(
                   out var connection,
                   out _))
        {
            // Candidate edges originate in the visited component.
            // Skip edges whose destination has since been reached.
            if (!visited.Add(connection.SecondNodeId))
            {
                continue;
            }

            selectedConnections.Add(connection);
            AddCandidates(connection.SecondNodeId);
        }

        var unreachableNodeIds = graph.GetNodes()
            .Where(node => !visited.Contains(node.Id))
            .Select(node => node.Id);

        return new MeshSpanningTreeResult(
            gatewayId,
            selectedConnections,
            visited,
            unreachableNodeIds);

        void AddCandidates(Guid nodeId)
        {
            foreach (var connection in
                     graph.GetAvailableConnections(nodeId))
            {
                if (visited.Contains(connection.SecondNodeId))
                {
                    continue;
                }

                candidates.Enqueue(
                    connection,
                    (
                        connection.EnergyCost,
                        connection.FirstNodeId,
                        connection.SecondNodeId
                    ));
            }
        }
    }
}

// Code attributes

/*
Title: Prim’s Algorithm for Minimum Spanning Tree (MST)
Author(s): GeeksforGeeks
Date: 2026
Version: Not applicable
Type: Online tutorial
Availability: https://www.geeksforgeeks.org/dsa/prims-minimum-spanning-tree-mst-greedy-algo-5/
*/

/*
Title: Priority Queue Data Structure
Author(s): Programiz
Date: n.d.
Version: Not applicable
Type: Online tutorial
Availability: https://www.programiz.com/dsa/priority-queue
*/