namespace SmartX.Core.Topology.Entities;

public sealed class MeshConnectionEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FirstNodeId { get; set; }

    public MeshNodeEntity FirstNode { get; set; } = null!;

    public Guid SecondNodeId { get; set; }

    public MeshNodeEntity SecondNode { get; set; } = null!;

    // Simulated transmission energy cost, not measured hardware usage.
    public double EnergyCost { get; set; }

    public bool IsEnabled { get; set; } = true;
}