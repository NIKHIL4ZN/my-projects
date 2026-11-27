using System.Collections.Concurrent;
using SmartX.Core.DTOs;
using SmartX.Core.Topology.DataStructures;

namespace SmartX.Infrastructure.Topology;

public sealed class TelemetryHistoryIndex
{
    private readonly ConcurrentDictionary<Guid, SensorHistory>
        _histories = new();

    public async Task<IReadOnlyList<TelemetryResponse>> QueryAsync(
        Guid sensorId,
        DateTime fromUtc,
        DateTime toUtc,
        Func<CancellationToken,
            Task<IReadOnlyList<TelemetryResponse>>> loadHistory,
        CancellationToken cancellationToken = default)
    {
        if (fromUtc > toUtc)
        {
            throw new ArgumentException(
                "The start timestamp must not exceed the end timestamp.");
        }

        var history = _histories.GetOrAdd(
            sensorId,
            _ => new SensorHistory());

        await history.Gate.WaitAsync(cancellationToken);

        try
        {
            if (history.Tree is null)
            {
                var readings = await loadHistory(cancellationToken);

                // Build locally so a failed load does not publish
                // an incomplete index.
                var tree =
                    new TelemetryHistoryTree<TelemetryResponse>();

                var readingIds = new HashSet<Guid>();

                foreach (var reading in readings)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (readingIds.Add(reading.Id))
                    {
                        tree.Insert(reading.RecordedAtUtc, reading);
                    }
                }

                history.ReadingIds = readingIds;
                history.Tree = tree;
            }

            return history.Tree.QueryRange(fromUtc, toUtc);
        }
        finally
        {
            history.Gate.Release();
        }
    }

    public async Task AppendAsync(
        Guid sensorId,
        IReadOnlyList<TelemetryResponse> readings)
    {
        // If nobody has queried this sensor yet, its first
        // query will load the saved readings from SQL Server.
        if (!_histories.TryGetValue(sensorId, out var history))
        {
            return;
        }

        // Saving has already succeeded. Complete the index
        // update even if the HTTP request was cancelled.
        await history.Gate.WaitAsync();

        try
        {
            if (history.Tree is null)
            {
                return;
            }

            foreach (var reading in readings)
            {
                // A concurrent initial load may already have
                // included this newly saved reading.
                if (history.ReadingIds.Add(reading.Id))
                {
                    history.Tree.Insert(
                        reading.RecordedAtUtc,
                        reading);
                }
            }
        }
        finally
        {
            history.Gate.Release();
        }
    }

    private sealed class SensorHistory
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);

        public TelemetryHistoryTree<TelemetryResponse>? Tree
        {
            get;
            set;
        }

        public HashSet<Guid> ReadingIds { get; set; } = [];
    }
}

// Code attributes

/*
Title: Cache Hit Rate Telemetry: Four Metrics Worth a Dashboard
Author(s): The PADISO Team
Date: 2026
Version: Not applicable
Type: Online guide
Availability: https://www.padiso.co/blog/cache-hit-rate-telemetry-posthog/
*/

/*
Title: SemaphoreSlim Class (System.Threading)
Author(s): Microsoft
Date: n.d.
Version: .NET 10.0
Type: Documentation
Availability: https://learn.microsoft.com/en-us/dotnet/api/system.threading.semaphoreslim?view=net-10.0
*/