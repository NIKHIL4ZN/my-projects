using System.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using SmartX.Core.Topology.Entities;
using SmartX.Infrastructure.Data;

namespace SmartX.Infrastructure.Topology;

public static class SmartXTopologySeeder
{
    private const string GatewayMacAddress =
        "02:00:00:00:00:01";

    public static async Task SeedAsync(
        SmartXDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var sensors = await dbContext.Sensors
            .AsNoTracking()
            .OrderBy(sensor => sensor.MacAddress)
            .ToListAsync(cancellationToken);

        var nodes = await dbContext.MeshNodes
            .ToListAsync(cancellationToken);

        var gateway = nodes.SingleOrDefault(node =>
            node.IsGateway &&
            node.MacAddress == GatewayMacAddress);

        if (gateway is null)
        {
            gateway = new MeshNodeEntity
            {
                Name = "Smart-X Central Gateway",
                MacAddress = GatewayMacAddress,
                DeploymentLocation = "Central Control Room",
                IsGateway = true,
                IsOnline = true
            };

            dbContext.MeshNodes.Add(gateway);
            nodes.Add(gateway);
        }

        var sensorNodesBySensorId = nodes
            .Where(node => node.SensorId.HasValue)
            .ToDictionary(
                node => node.SensorId!.Value);

        foreach (var sensor in sensors)
        {
            if (sensorNodesBySensorId.ContainsKey(sensor.Id))
            {
                continue;
            }

            var node = new MeshNodeEntity
            {
                SensorId = sensor.Id,
                Name = sensor.Name,
                MacAddress = sensor.MacAddress,
                DeploymentLocation = sensor.DeploymentLocation,
                IsGateway = false,
                IsOnline = true
            };

            dbContext.MeshNodes.Add(node);
            nodes.Add(node);
            sensorNodesBySensorId.Add(sensor.Id, node);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // Preserve an existing configured network on later startups.
        if (await dbContext.MeshConnections
                .AnyAsync(cancellationToken))
        {
            return;
        }

        var sensorNodes = sensors
            .Select(sensor => sensorNodesBySensorId[sensor.Id])
            .ToList();

        var addedPairs = new HashSet<(Guid First, Guid Second)>();

        // A chain gives devices multi-hop routes through neighbours.
        for (var index = 0; index < sensorNodes.Count - 1; index++)
        {
            AddConnection(
                sensorNodes[index].Id,
                sensorNodes[index + 1].Id,
                1.0 + (index % 3) * 0.25);
        }

        if (sensorNodes.Count > 0)
        {
            AddConnection(
                gateway.Id,
                sensorNodes[0].Id,
                1.0);
        }

        if (sensorNodes.Count > 1)
        {
            // The opposite end also connects to the gateway,
            // providing an alternative around chain failures.
            AddConnection(
                gateway.Id,
                sensorNodes[^1].Id,
                2.5);
        }

        // Extra links create cycles and alternate relay routes.
        for (var index = 0; index < sensorNodes.Count - 2; index++)
        {
            AddConnection(
                sensorNodes[index].Id,
                sensorNodes[index + 2].Id,
                2.0 + (index % 2) * 0.5);
        }

        if (sensorNodes.Count > 3)
        {
            AddConnection(
                gateway.Id,
                sensorNodes[sensorNodes.Count / 2].Id,
                4.0);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        void AddConnection(
            Guid firstNodeId,
            Guid secondNodeId,
            double energyCost)
        {
            if (firstNodeId == secondNodeId)
            {
                return;
            }

            // SqlGuid uses SQL Server's uniqueidentifier ordering.
            // This matches the database endpoint-order constraint.
            if (new SqlGuid(firstNodeId).CompareTo(
                    new SqlGuid(secondNodeId)) > 0)
            {
                (firstNodeId, secondNodeId) =
                    (secondNodeId, firstNodeId);
            }

            if (!addedPairs.Add((firstNodeId, secondNodeId)))
            {
                return;
            }

            dbContext.MeshConnections.Add(
                new MeshConnectionEntity
                {
                    FirstNodeId = firstNodeId,
                    SecondNodeId = secondNodeId,
                    EnergyCost = energyCost,
                    IsEnabled = true
                });
        }
    }
}