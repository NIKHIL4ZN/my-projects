using SmartX.Core.Entities;

namespace SmartX.Core.Topology.Entities;

public sealed class MeshNodeEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Null for a gateway; populated for a sensor node.
    public Guid? SensorId { get; set; }

    public Sensor? Sensor { get; set; }

    public required string Name { get; set; }

    public required string MacAddress { get; set; }

    public required string DeploymentLocation { get; set; }

    public bool IsGateway { get; set; }

    public bool IsOnline { get; set; } = true;
}