namespace SmartX.Core.Topology.Models;

public sealed class MeshTraversalResult
{
    private readonly Dictionary<Guid, Guid?> _parents;
    private readonly Dictionary<Guid, int> _hopCounts;

    public MeshTraversalResult(
        Guid startNodeId,
        IDictionary<Guid, Guid?> parents,
        IDictionary<Guid, int> hopCounts,
        IEnumerable<Guid> unreachableNodeIds)
    {
        StartNodeId = startNodeId;
        _parents = new Dictionary<Guid, Guid?>(parents);
        _hopCounts = new Dictionary<Guid, int>(hopCounts);
        UnreachableNodeIds = unreachableNodeIds.ToList().AsReadOnly();
    }

    public Guid StartNodeId { get; }

    public IReadOnlyList<Guid> UnreachableNodeIds { get; }

    public IReadOnlyList<Guid> ReachableNodeIds =>
        _hopCounts.Keys.OrderBy(id => id).ToList();

    public int? GetHopCount(Guid nodeId)
    {
        return _hopCounts.TryGetValue(nodeId, out var count)
            ? count
            : null;
    }

    public IReadOnlyList<Guid> GetPathTo(Guid nodeId)
    {
        if (!_parents.ContainsKey(nodeId))
        {
            return Array.Empty<Guid>();
        }

        var path = new List<Guid>();
        Guid? current = nodeId;

        while (current.HasValue)
        {
            path.Add(current.Value);
            current = _parents[current.Value];
        }

        path.Reverse();

        return path;
    }
}