using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SmartX.Core.Commands.DTOs;
using SmartX.Core.Commands.Enums;
using SmartX.Core.Commands.Interfaces;
using SmartX.Core.Entities;
using SmartX.Core.Enums;
using SmartX.Infrastructure.Data;

namespace SmartX.Infrastructure.Commands;

public class PredictiveActionService
    : IPredictiveActionService
{
    private static readonly TimeSpan
        DisconnectionWindow =
            TimeSpan.FromMinutes(2);

    private readonly SmartXDbContext _dbContext;

    private readonly IOperatorQueryTracker
        _queryTracker;

    public PredictiveActionService(
        SmartXDbContext dbContext,
        IOperatorQueryTracker queryTracker)
    {
        _dbContext = dbContext;
        _queryTracker = queryTracker;
    }

    public async Task<IReadOnlyList<
        ActionRecommendationResponse>>
        GetRecommendationsAsync(
            string? operatorQuery = null,
            CancellationToken cancellationToken =
                default)
    {
        if (!string.IsNullOrWhiteSpace(
                operatorQuery))
        {
            _queryTracker.RecordQuery(
                operatorQuery);
        }

        var sensors = await _dbContext.Sensors
            .AsNoTracking()
            .OrderBy(sensor => sensor.Name)
            .ToListAsync(cancellationToken);

        var recentReadings =
            await _dbContext.TelemetryReadings
                .AsNoTracking()
                .OrderByDescending(
                    reading =>
                        reading.RecordedAtUtc)
                .Take(1000)
                .ToListAsync(cancellationToken);

        var readingsBySensor = recentReadings
            .GroupBy(reading => reading.SensorId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(
                        reading =>
                            reading.RecordedAtUtc)
                    .ToList());

        var frequentKeywords =
            _queryTracker.GetFrequentPatterns()
                .ToDictionary(
                    pattern => pattern.Keyword,
                    pattern => pattern.SearchCount,
                    StringComparer.OrdinalIgnoreCase);

        var recommendations =
            new List<ActionRecommendationResponse>();

        var recommendationKeys =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var sensor in sensors)
        {
            readingsBySensor.TryGetValue(
                sensor.Id,
                out var sensorReadings);

            sensorReadings ??=
                new List<TelemetryReading>();

            AddConnectivityRecommendation(
                recommendations,
                recommendationKeys,
                sensor);

            AddTelemetryRecommendations(
                recommendations,
                recommendationKeys,
                sensor,
                sensorReadings);

            AddQueryPatternRecommendation(
                recommendations,
                recommendationKeys,
                sensor,
                frequentKeywords);
        }

        return recommendations
            .OrderByDescending(
                recommendation =>
                    recommendation.Priority)
            .ThenByDescending(
                recommendation =>
                    recommendation.ConfidencePercent)
            .ThenBy(
                recommendation =>
                    recommendation.SensorName)
            .ToList();
    }

    private static void
        AddConnectivityRecommendation(
            ICollection<
                ActionRecommendationResponse>
                recommendations,
            ISet<string> recommendationKeys,
            Sensor sensor)
    {
        if (sensor.LastSeenAtUtc is null)
        {
            AddRecommendation(
                recommendations,
                recommendationKeys,
                sensor,
                "missing-initial-telemetry",
                "Verify initial sensor connectivity",
                "This registered sensor has not " +
                "submitted its first telemetry packet.",
                DeviceCommandType.RequestStatus,
                CommandPriority.High,
                92,
                "Request the sensor's current " +
                "connection and runtime status.",
                [
                    "No last-contact timestamp is available.",
                    $"Sensor status: {sensor.Status}",
                    $"MAC address: {sensor.MacAddress}"
                ]);

            return;
        }

        var silentDuration =
            DateTime.UtcNow -
            sensor.LastSeenAtUtc.Value;

        if (silentDuration >
            DisconnectionWindow)
        {
            AddRecommendation(
                recommendations,
                recommendationKeys,
                sensor,
                "sensor-silent",
                "Recover silent sensor",
                "The sensor has stopped reporting " +
                "within the expected two-minute window.",
                DeviceCommandType.Restart,
                CommandPriority.Critical,
                98,
                "Restart the device after checking " +
                "gateway connectivity and power.",
                [
                    $"Last contact: " +
                    $"{sensor.LastSeenAtUtc:O}",
                    $"Silent for: " +
                    $"{silentDuration.TotalMinutes:F1} minutes",
                    $"Location: " +
                    $"{sensor.DeploymentLocation}"
                ]);
        }
    }

    private static void
        AddTelemetryRecommendations(
            ICollection<
                ActionRecommendationResponse>
                recommendations,
            ISet<string> recommendationKeys,
            Sensor sensor,
            IReadOnlyList<TelemetryReading>
                readings)
    {
        if (readings.Count == 0)
        {
            return;
        }

        var latestReading = readings[0];

        var latestValue =
            GetNumericValue(latestReading);

        if (latestValue is null)
        {
            return;
        }

        if (
            sensor.Name.Contains(
                "Moisture",
                StringComparison.OrdinalIgnoreCase) &&
            latestValue <= 20)
        {
            AddRecommendation(
                recommendations,
                recommendationKeys,
                sensor,
                "critical-moisture",
                "Restore irrigation flow",
                "The latest moisture value is below " +
                "the critical operating threshold.",
                DeviceCommandType.Activate,
                CommandPriority.Critical,
                97,
                "Activate the assigned irrigation " +
                "system after verifying the probe.",
                [
                    $"Current moisture: " +
                    $"{latestValue:F1}%",
                    "Critical threshold: 20.0%",
                    $"Reading time: " +
                    $"{latestReading.RecordedAtUtc:O}"
                ]);
        }

        if (
            sensor.Category ==
                SensorCategory.PowerConsumption &&
            latestValue >= 2000)
        {
            AddRecommendation(
                recommendations,
                recommendationKeys,
                sensor,
                "power-spike",
                "Inspect excessive power consumption",
                "The latest power reading exceeds " +
                "the configured high-usage threshold.",
                DeviceCommandType.Deactivate,
                CommandPriority.Critical,
                95,
                "Deactivate the connected load if " +
                "the reading is confirmed as unsafe.",
                [
                    $"Current reading: " +
                    $"{latestValue:F1} " +
                    $"{latestReading.Unit}",
                    "High-usage threshold: 2000 W",
                    $"Location: " +
                    $"{sensor.DeploymentLocation}"
                ]);
        }

        if (readings.Count < 2)
        {
            return;
        }

        var previousValue =
            GetNumericValue(readings[1]);

        if (previousValue is null)
        {
            return;
        }

        var denominator =
            Math.Max(
                Math.Abs(previousValue.Value),
                1);

        var changePercent =
            Math.Abs(
                latestValue.Value -
                previousValue.Value) /
            denominator * 100;

        if (changePercent >= 40)
        {
            AddRecommendation(
                recommendations,
                recommendationKeys,
                sensor,
                "sudden-change",
                "Validate sensor calibration",
                "Two consecutive readings changed " +
                "more rapidly than expected.",
                DeviceCommandType.Calibrate,
                CommandPriority.High,
                Math.Min(
                    95,
                    75 + changePercent / 5),
                "Run a calibration check before " +
                "trusting the abnormal reading.",
                [
                    $"Previous value: " +
                    $"{previousValue:F2}",
                    $"Current value: " +
                    $"{latestValue:F2}",
                    $"Absolute change: " +
                    $"{changePercent:F1}%"
                ]);
        }
    }

    private static void
        AddQueryPatternRecommendation(
            ICollection<
                ActionRecommendationResponse>
                recommendations,
            ISet<string> recommendationKeys,
            Sensor sensor,
            IReadOnlyDictionary<string, int>
                keywordFrequency)
    {
        var powerSearches =
            GetCombinedFrequency(
                keywordFrequency,
                "power",
                "energy",
                "electricity");

        if (
            powerSearches >= 2 &&
            sensor.Category ==
                SensorCategory.PowerConsumption)
        {
            AddRecommendation(
                recommendations,
                recommendationKeys,
                sensor,
                "query-power-interest",
                "Inspect frequently queried power device",
                "The operator repeatedly searched for " +
                "power-related information.",
                DeviceCommandType.RequestStatus,
                CommandPriority.Normal,
                Math.Min(
                    90,
                    60 + powerSearches * 5),
                "Request an updated status packet " +
                "from this power sensor.",
                [
                    $"Power-related searches: " +
                    $"{powerSearches}",
                    $"Sensor: {sensor.Name}",
                    $"Location: " +
                    $"{sensor.DeploymentLocation}"
                ]);
        }

        var connectionSearches =
            GetCombinedFrequency(
                keywordFrequency,
                "offline",
                "disconnect",
                "connection",
                "gateway");

        if (
            connectionSearches >= 2 &&
            sensor.LastSeenAtUtc is not null)
        {
            AddRecommendation(
                recommendations,
                recommendationKeys,
                sensor,
                "query-connectivity-interest",
                "Check device connectivity",
                "Repeated connectivity searches " +
                "indicate that the operator may be " +
                "investigating device availability.",
                DeviceCommandType.RequestStatus,
                CommandPriority.Normal,
                Math.Min(
                    90,
                    60 + connectionSearches * 5),
                "Request the device's current " +
                "connectivity status.",
                [
                    $"Connectivity searches: " +
                    $"{connectionSearches}",
                    $"Last contact: " +
                    $"{sensor.LastSeenAtUtc:O}",
                    $"Sensor status: {sensor.Status}"
                ]);
        }
    }

    private static int GetCombinedFrequency(
        IReadOnlyDictionary<string, int>
            frequencies,
        params string[] keywords)
    {
        var total = 0;

        foreach (var keyword in keywords)
        {
            if (frequencies.TryGetValue(
                    keyword,
                    out var count))
            {
                total += count;
            }
        }

        return total;
    }

    private static double?
        GetNumericValue(
            TelemetryReading reading)
    {
        if (reading.FloatValue is not null)
        {
            return reading.FloatValue.Value;
        }

        if (reading.IntegerValue is not null)
        {
            return reading.IntegerValue.Value;
        }

        return null;
    }

    private static void AddRecommendation(
        ICollection<
            ActionRecommendationResponse>
            recommendations,
        ISet<string> recommendationKeys,
        Sensor sensor,
        string ruleKey,
        string title,
        string explanation,
        DeviceCommandType commandType,
        CommandPriority priority,
        double confidence,
        string payload,
        IReadOnlyList<string> evidence)
    {
        var uniqueKey =
            $"{sensor.Id}:{ruleKey}";

        if (!recommendationKeys.Add(uniqueKey))
        {
            return;
        }

        recommendations.Add(
            new ActionRecommendationResponse
            {
                RecommendationId =
                    CreateDeterministicId(
                        uniqueKey),

                SensorId = sensor.Id,
                SensorName = sensor.Name,
                Title = title,
                Explanation = explanation,

                RecommendedCommandType =
                    commandType,

                Priority = priority,

                ConfidencePercent =
                    Math.Round(
                        confidence,
                        1),

                SuggestedPayload = payload,
                Evidence = evidence
            });
    }

    private static Guid CreateDeterministicId(
        string value)
    {
        var bytes =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(value));

        return new Guid(
            bytes.AsSpan(0, 16));
    }
}