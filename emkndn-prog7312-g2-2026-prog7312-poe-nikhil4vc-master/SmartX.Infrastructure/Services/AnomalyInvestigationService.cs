using SmartX.Core.DataStructures;
using SmartX.Core.DTOs;
using SmartX.Core.Entities;
using SmartX.Core.Enums;
using SmartX.Core.Interfaces;
using SmartX.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;


namespace SmartX.Infrastructure.Services
{
    public class AnomalyInvestigationService
     : IAnomalyInvestigationService
    {
        private readonly SmartXDbContext _dbContext;

        public AnomalyInvestigationService(
            SmartXDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<
            AnomalyInvestigationResponse>> AnalyseAsync(
            CancellationToken cancellationToken = default)
        {
            var sensors = await _dbContext.Sensors
                .AsNoTracking()
                .Include(sensor =>
                    sensor.TelemetryReadings
                        .OrderByDescending(
                            reading =>
                                reading.RecordedAtUtc)
                        .Take(2))
                .OrderBy(sensor => sensor.Name)
                .ToListAsync(cancellationToken);

            var investigations =
                new List<AnomalyInvestigationResponse>();

            var totalPower = CalculateAggregatePower(
                sensors);

            foreach (var sensor in sensors)
            {
                var readings = sensor.TelemetryReadings
                    .OrderByDescending(
                        reading => reading.RecordedAtUtc)
                    .ToList();

                if (readings.Count == 0)
                {
                    investigations.Add(
                        CreateMissingTelemetryInvestigation(
                            sensor));

                    continue;
                }

                DetectDisconnect(
                    sensor,
                    investigations);

                DetectMoistureProblem(
                    sensor,
                    readings[0],
                    investigations);

                DetectPowerProblem(
                    sensor,
                    readings[0],
                    totalPower,
                    investigations);

                if (readings.Count >= 2)
                {
                    DetectSuddenChange(
                        sensor,
                        readings[0],
                        readings[1],
                        investigations);
                }
            }

            return investigations
                .OrderByDescending(
                    item => item.Severity)
                .ThenByDescending(
                    item => item.DetectedAtUtc)
                .ToList();
        }

        private static PowerMetric CalculateAggregatePower(
            IEnumerable<Sensor> sensors)
        {
            var aggregate = new PowerMetric(0);

            foreach (var sensor in sensors)
            {
                var latest = sensor.TelemetryReadings
                    .OrderByDescending(
                        reading => reading.RecordedAtUtc)
                    .FirstOrDefault();

                if (latest?.IntegerValue is int watts
                    && watts >= 0
                    && string.Equals(
                        latest.Unit,
                        "W",
                        StringComparison.OrdinalIgnoreCase))
                {
                    aggregate += new PowerMetric(watts);
                }
            }

            return aggregate;
        }

        private static void DetectDisconnect(
            Sensor sensor,
            List<AnomalyInvestigationResponse>
                investigations)
        {
            if (!sensor.LastSeenAtUtc.HasValue)
            {
                return;
            }

            var silentDuration =
                DateTime.UtcNow - sensor.LastSeenAtUtc.Value;

            if (silentDuration <= TimeSpan.FromMinutes(2))
            {
                return;
            }

            investigations.Add(new AnomalyInvestigationResponse
            {
                InvestigationId =
                    CreateStableId(sensor.Id, "disconnect"),
                SensorId = sensor.Id,
                SensorName = sensor.Name,
                DeploymentLocation =
                    sensor.DeploymentLocation,
                Severity = AnomalySeverity.Critical,
                Title = "Sensor telemetry has stopped",
                Explanation =
                    "The gateway has not received telemetry " +
                    "within the expected two-minute window.",
                DetectedAtUtc = sensor.LastSeenAtUtc.Value,
                ConfidencePercent = 98,
                Evidence =
                [
                    $"Last contact: {sensor.LastSeenAtUtc:O}",
                $"Silent for: {silentDuration.TotalMinutes:F1} minutes",
                $"MAC address: {sensor.MacAddress}"
                ],
                Steps =
                [
                    CreateStep(
                    1,
                    "Check gateway connection",
                    "Confirm that the gateway serving this " +
                    "deployment location is online.",
                    "CheckConnection"),
                CreateStep(
                    2,
                    "Inspect sensor power",
                    "Verify the ESP32 power supply, battery, " +
                    "and physical indicators.",
                    "VerifyHardware"),
                CreateStep(
                    3,
                    "Review the last packets",
                    "Inspect telemetry immediately before " +
                    "the disconnect for abnormal values.",
                    "InspectTelemetry")
                ]
            });
        }

        private static void DetectMoistureProblem(
            Sensor sensor,
            TelemetryReading latest,
            List<AnomalyInvestigationResponse>
                investigations)
        {
            if (latest.FloatValue is not double value
                || !string.Equals(
                    latest.Unit,
                    "%",
                    StringComparison.OrdinalIgnoreCase)
                || !sensor.Name.Contains(
                    "moisture",
                    StringComparison.OrdinalIgnoreCase)
                || value >= 30)
            {
                return;
            }

            var critical = value < 20;

            investigations.Add(new AnomalyInvestigationResponse
            {
                InvestigationId =
                    CreateStableId(sensor.Id, "moisture-low"),
                SensorId = sensor.Id,
                SensorName = sensor.Name,
                DeploymentLocation =
                    sensor.DeploymentLocation,
                Severity = critical
                    ? AnomalySeverity.Critical
                    : AnomalySeverity.Warning,
                Title = critical
                    ? "Critical moisture drop detected"
                    : "Moisture is approaching its lower limit",
                Explanation =
                    "The current moisture reading is below the " +
                    "recommended hydroponics operating range.",
                DetectedAtUtc = latest.RecordedAtUtc,
                ConfidencePercent = critical ? 97 : 88,
                Evidence =
                [
                    $"Current moisture: {value:F1}%",
                "Warning threshold: 30.0%",
                "Critical threshold: 20.0%"
                ],
                Steps =
                [
                    CreateStep(
                    1,
                    "Inspect recent moisture history",
                    "Confirm whether this is a sustained " +
                    "decline or a single abnormal packet.",
                    "InspectTelemetry"),
                CreateStep(
                    2,
                    "Check pump and valve state",
                    "Verify irrigation flow and ensure the " +
                    "assigned actuator is responsive.",
                    "VerifyHardware"),
                CreateStep(
                    3,
                    "Inspect the probe",
                    "Check probe placement, wiring, and " +
                    "calibration before overriding a pump.",
                    "ReviewConfiguration")
                ]
            });
        }

        private static void DetectPowerProblem(
            Sensor sensor,
            TelemetryReading latest,
            PowerMetric aggregatePower,
            List<AnomalyInvestigationResponse>
                investigations)
        {
            if (latest.IntegerValue is not int watts
                || watts < 0
                || !string.Equals(
                    latest.Unit,
                    "W",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var currentPower =
                new PowerMetric(watts);

            var warningThreshold =
                new PowerMetric(1500);

            var criticalThreshold =
                new PowerMetric(2000);

            if (currentPower < warningThreshold)
            {
                return;
            }

            var critical =
                currentPower >= criticalThreshold;

            investigations.Add(new AnomalyInvestigationResponse
            {
                InvestigationId =
                    CreateStableId(sensor.Id, "power-high"),
                SensorId = sensor.Id,
                SensorName = sensor.Name,
                DeploymentLocation =
                    sensor.DeploymentLocation,
                Severity = critical
                    ? AnomalySeverity.Critical
                    : AnomalySeverity.Warning,
                Title = critical
                    ? "Critical power spike detected"
                    : "Power consumption is elevated",
                Explanation =
                    "The smart meter has exceeded its expected " +
                    "operating threshold.",
                DetectedAtUtc = latest.RecordedAtUtc,
                ConfidencePercent = critical ? 96 : 86,
                Evidence =
                [
                    $"Current load: {currentPower}",
                $"Aggregate active load: {aggregatePower}",
                $"Critical threshold: {criticalThreshold}"
                ],
                Steps =
                [
                    CreateStep(
                    1,
                    "Inspect the load history",
                    "Compare the spike with recent meter " +
                    "readings and connected equipment.",
                    "InspectTelemetry"),
                CreateStep(
                    2,
                    "Check connected equipment",
                    "Identify equipment that started shortly " +
                    "before the spike.",
                    "VerifyHardware"),
                CreateStep(
                    3,
                    "Review safety configuration",
                    "Confirm breaker limits and automated " +
                    "shutdown thresholds.",
                    "ReviewConfiguration")
                ]
            });
        }

        private static void DetectSuddenChange(
            Sensor sensor,
            TelemetryReading latest,
            TelemetryReading previous,
            List<AnomalyInvestigationResponse>
                investigations)
        {
            var latestValue = GetNumericValue(latest);
            var previousValue = GetNumericValue(previous);

            if (!latestValue.HasValue
                || !previousValue.HasValue
                || Math.Abs(previousValue.Value) < 0.001)
            {
                return;
            }

            var percentageChange =
                Math.Abs(
                    (latestValue.Value
                        - previousValue.Value)
                    / previousValue.Value)
                * 100;

            if (percentageChange < 30)
            {
                return;
            }

            investigations.Add(new AnomalyInvestigationResponse
            {
                InvestigationId =
                    CreateStableId(sensor.Id, "sudden-change"),
                SensorId = sensor.Id,
                SensorName = sensor.Name,
                DeploymentLocation =
                    sensor.DeploymentLocation,
                Severity = percentageChange >= 60
                    ? AnomalySeverity.Critical
                    : AnomalySeverity.Warning,
                Title = "Sudden telemetry change detected",
                Explanation =
                    "Two consecutive readings changed more " +
                    "rapidly than the expected operating pattern.",
                DetectedAtUtc = latest.RecordedAtUtc,
                ConfidencePercent =
                    Math.Min(99, 70 + percentageChange / 4),
                Evidence =
                [
                    $"Previous value: {previousValue:F2} {latest.Unit}",
                $"Current value: {latestValue:F2} {latest.Unit}",
                $"Absolute change: {percentageChange:F1}%"
                ],
                Steps =
                [
                    CreateStep(
                    1,
                    "Compare neighbouring sensors",
                    "Check whether nearby devices show the " +
                    "same environmental change.",
                    "InspectTelemetry"),
                CreateStep(
                    2,
                    "Validate sensor calibration",
                    "Inspect the sensor probe and calibration " +
                    "profile for drift.",
                    "ReviewConfiguration"),
                CreateStep(
                    3,
                    "Check physical conditions",
                    "Confirm whether a real environmental or " +
                    "electrical event occurred.",
                    "VerifyHardware")
                ]
            });
        }

        private static AnomalyInvestigationResponse
            CreateMissingTelemetryInvestigation(
                Sensor sensor)
        {
            return new AnomalyInvestigationResponse
            {
                InvestigationId =
                    CreateStableId(sensor.Id, "no-telemetry"),
                SensorId = sensor.Id,
                SensorName = sensor.Name,
                DeploymentLocation =
                    sensor.DeploymentLocation,
                Severity = AnomalySeverity.Warning,
                Title = "Sensor is awaiting initial telemetry",
                Explanation =
                    "The sensor is registered but has not yet " +
                    "published its first packet.",
                DetectedAtUtc = sensor.CreatedAtUtc,
                ConfidencePercent = 100,
                Evidence =
                [
                    $"Registered: {sensor.CreatedAtUtc:O}",
                $"Expected data type: {sensor.DataType}",
                $"MAC address: {sensor.MacAddress}"
                ],
                Steps =
                [
                    CreateStep(
                    1,
                    "Verify device configuration",
                    "Confirm that the sensor uses the correct " +
                    "gateway URL and MAC address.",
                    "ReviewConfiguration"),
                CreateStep(
                    2,
                    "Check network connectivity",
                    "Confirm that the ESP32 can reach the " +
                    "ingestion API.",
                    "CheckConnection"),
                CreateStep(
                    3,
                    "Send a test packet",
                    "Publish one typed telemetry packet and " +
                    "confirm that it appears in history.",
                    "InspectTelemetry")
                ]
            };
        }

        private static double? GetNumericValue(
            TelemetryReading reading)
        {
            if (reading.FloatValue.HasValue)
            {
                return reading.FloatValue.Value;
            }

            if (reading.IntegerValue.HasValue)
            {
                return reading.IntegerValue.Value;
            }

            return null;
        }

        private static InvestigationStepResponse CreateStep(
            int order,
            string title,
            string guidance,
            string actionType)
        {
            return new InvestigationStepResponse
            {
                Order = order,
                Title = title,
                Guidance = guidance,
                ActionType = actionType
            };
        }

        private static Guid CreateStableId(
            Guid sensorId,
            string anomalyType)
        {
            var source =
                $"{sensorId:N}:{anomalyType}";

            var hash = SHA256.HashData(
                Encoding.UTF8.GetBytes(source));

            return new Guid(hash.AsSpan(0, 16));
        }
    }
}
