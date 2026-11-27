namespace SmartX.Core.Topology.Models;

public sealed class MeshSpanningTreeResult
{
    public MeshSpanningTreeResult(
        Guid gatewayId,
        IEnumerable<MeshConnection> connections,
        IEnumerable<Guid> connectedNodeIds,
        IEnumerable<Guid> unreachableNodeIds)
    {
        GatewayId = gatewayId;
        Connections = connections.ToList().AsReadOnly();
        ConnectedNodeIds = connectedNodeIds
            .OrderBy(id => id)
            .ToList()
            .AsReadOnly();

        UnreachableNodeIds = unreachableNodeIds
            .OrderBy(id => id)
            .ToList()
            .AsReadOnly();
    }

    public Guid GatewayId { get; }

    public IReadOnlyList<MeshConnection> Connections { get; }

    public IReadOnlyList<Guid> ConnectedNodeIds { get; }

    public IReadOnlyList<Guid> UnreachableNodeIds { get; }

    public double TotalEnergyCost =>
        Connections.Sum(connection => connection.EnergyCost);

    public bool SpansEntireNetwork =>
        UnreachableNodeIds.Count == 0;
}