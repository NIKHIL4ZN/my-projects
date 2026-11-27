using SmartX.Core.Entities;
using SmartX.Core.Enums;
using SmartX.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;


namespace SmartX.Infrastructure
{
    public static class SmartXDataSeeder
    {
        public static async Task SeedAsync(
            SmartXDbContext dbContext,
            CancellationToken cancellationToken = default)
        {
            var definitions = CreateDefinitions();

            foreach (var definition in definitions)
            {
                var sensor = await dbContext.Sensors
                    .Include(item => item.TelemetryReadings)
                    .SingleOrDefaultAsync(
                        item =>
                            item.MacAddress ==
                            definition.MacAddress,
                        cancellationToken);

                if (sensor is null)
                {
                    sensor = new Sensor
                    {
                        Name = definition.Name,
                        MacAddress = definition.MacAddress,
                        DeploymentLocation =
                            definition.Location,
                        Category = definition.Category,
                        DataType = definition.DataType,
                        Status = SensorStatus.Online,
                        CreatedAtUtc =
                            DateTime.UtcNow.AddDays(-7)
                    };

                    dbContext.Sensors.Add(sensor);
                }

                sensor.Status = SensorStatus.Online;
                sensor.LastSeenAtUtc =
                    DateTime.UtcNow.AddSeconds(-20);

                if (sensor.TelemetryReadings.Count == 0)
                {
                    AddTelemetry(
                        sensor,
                        definition);
                }
            }

            await dbContext.SaveChangesAsync(
                cancellationToken);
        }

        private static IReadOnlyList<
            SensorSeedDefinition> CreateDefinitions()
        {
            return
            [
                new(
                "Greenhouse Temperature A",
                "A4:CF:12:9B:20:10",
                "Hydroponics Facility / Zone 1",
                SensorCategory.Environmental,
                SensorDataType.Float,
                "°C",
                23.5),

            new(
                "Greenhouse Humidity A",
                "A4:CF:12:9B:20:11",
                "Hydroponics Facility / Zone 1",
                SensorCategory.Environmental,
                SensorDataType.Float,
                "%",
                64.0),

            new(
                "Solar Array Power Meter",
                "A4:CF:12:9B:20:12",
                "Smart Grid / Solar Array",
                SensorCategory.PowerConsumption,
                SensorDataType.Integer,
                "W",
                1450),

            new(
                "Irrigation Valve A",
                "A4:CF:12:9B:20:13",
                "Hydroponics Facility / Zone 1",
                SensorCategory.Actuator,
                SensorDataType.Boolean,
                "state",
                1),

            new(
                "Cold Room Temperature",
                "A4:CF:12:9B:20:14",
                "Smart Building / Cold Room",
                SensorCategory.Environmental,
                SensorDataType.Float,
                "°C",
                4.2),

            new(
                "Facility Power Meter",
                "A4:CF:12:9B:20:15",
                "Smart Building / Main Distribution",
                SensorCategory.PowerConsumption,
                SensorDataType.Integer,
                "W",
                1850)
            ];
        }

        private static void AddTelemetry(
            Sensor sensor,
            SensorSeedDefinition definition)
        {
            const int readingCount = 48;

            var random = new Random(
                definition.MacAddress.GetHashCode());

            var startTime =
                DateTime.UtcNow.AddHours(-4);

            for (var index = 0;
                 index < readingCount;
                 index++)
            {
                var reading = new TelemetryReading
                {
                    SensorId = sensor.Id,
                    DataType = definition.DataType,
                    Unit = definition.Unit,
                    SequenceNumber = index,
                    RecordedAtUtc =
                        startTime.AddMinutes(index * 5)
                };

                switch (definition.DataType)
                {
                    case SensorDataType.Float:
                        reading.FloatValue =
                            Math.Round(
                                definition.BaseValue
                                + random.NextDouble() * 2
                                - 1,
                                2);
                        break;

                    case SensorDataType.Integer:
                        reading.IntegerValue =
                            Math.Max(
                                0,
                                (int)Math.Round(
                                    definition.BaseValue
                                    + random.Next(-120, 121)));
                        break;

                    case SensorDataType.Boolean:
                        reading.BooleanValue =
                            index % 8 < 5;
                        break;

                    default:
                        throw new InvalidOperationException(
                            "Unsupported seeded data type.");
                }

                sensor.TelemetryReadings.Add(reading);
            }
        }

        private sealed record SensorSeedDefinition(
            string Name,
            string MacAddress,
            string Location,
            SensorCategory Category,
            SensorDataType DataType,
            string Unit,
            double BaseValue);
    }
}
