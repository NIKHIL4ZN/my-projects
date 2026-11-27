using SmartX.Core.DTOs;
using SmartX.Infrastructure.Topology;
using Xunit;

namespace SmartX.Api.Tests.Topology;

public class TelemetryHistoryIndexTests
{
    private static readonly DateTime Start =
        new(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task QueryAsync_LoadsHistoryOnlyOnce()
    {
        var index = new TelemetryHistoryIndex();
        var sensorId = Guid.NewGuid();
        var reading = CreateReading(sensorId, Start);
        var loadCount = 0;

        Task<IReadOnlyList<TelemetryResponse>> Load(
            CancellationToken token)
        {
            loadCount++;

            return Task.FromResult<IReadOnlyList<TelemetryResponse>>(
                new[] { reading });
        }

        var first = await index.QueryAsync(
            sensorId, Start, Start.AddHours(1), Load);

        var second = await index.QueryAsync(
            sensorId, Start, Start.AddHours(1), Load);

        Assert.Equal(1, loadCount);
        Assert.Equal(reading.Id, Assert.Single(first).Id);
        Assert.Equal(reading.Id, Assert.Single(second).Id);
    }

    [Fact]
    public async Task AppendAsync_UpdatesLoadedHistoryWithoutDuplicates()
    {
        var index = new TelemetryHistoryIndex();
        var sensorId = Guid.NewGuid();

        var first = CreateReading(sensorId, Start);
        var second = CreateReading(
            sensorId, Start.AddMinutes(1));

        Task<IReadOnlyList<TelemetryResponse>> Load(
            CancellationToken token)
        {
            return Task.FromResult<IReadOnlyList<TelemetryResponse>>(
                new[] { first });
        }

        await index.QueryAsync(
            sensorId, Start, Start.AddHours(1), Load);

        await index.AppendAsync(
            sensorId, new[] { first, second });

        await index.AppendAsync(
            sensorId, new[] { second });

        var results = await index.QueryAsync(
            sensorId, Start, Start.AddHours(1), Load);

        Assert.Equal(
            new[] { first.Id, second.Id },
            results.Select(reading => reading.Id));
    }

    [Fact]
    public async Task QueryAsync_KeepsSensorHistoriesSeparate()
    {
        var index = new TelemetryHistoryIndex();

        var firstSensorId = Guid.NewGuid();
        var secondSensorId = Guid.NewGuid();

        var firstReading = CreateReading(firstSensorId, Start);
        var secondReading = CreateReading(secondSensorId, Start);

        await index.QueryAsync(
            firstSensorId,
            Start,
            Start,
            _ => Task.FromResult<IReadOnlyList<TelemetryResponse>>(
                new[] { firstReading }));

        var results = await index.QueryAsync(
            secondSensorId,
            Start,
            Start,
            _ => Task.FromResult<IReadOnlyList<TelemetryResponse>>(
                new[] { secondReading }));

        Assert.Equal(
            secondSensorId,
            Assert.Single(results).SensorId);
    }

    [Fact]
    public async Task QueryAsync_RetriesAfterFailedInitialLoad()
    {
        var index = new TelemetryHistoryIndex();
        var sensorId = Guid.NewGuid();
        var reading = CreateReading(sensorId, Start);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            index.QueryAsync(
                sensorId,
                Start,
                Start,
                _ => Task.FromException<
                    IReadOnlyList<TelemetryResponse>>(
                    new InvalidOperationException("Load failed."))));

        var results = await index.QueryAsync(
            sensorId,
            Start,
            Start,
            _ => Task.FromResult<IReadOnlyList<TelemetryResponse>>(
                new[] { reading }));

        Assert.Equal(reading.Id, Assert.Single(results).Id);
    }

    [Fact]
    public async Task QueryAsync_ConcurrentInitialQueriesShareOneLoad()
    {
        var index = new TelemetryHistoryIndex();
        var sensorId = Guid.NewGuid();
        var reading = CreateReading(sensorId, Start);
        var loadCount = 0;

        var loadStarted = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var releaseLoad = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<IReadOnlyList<TelemetryResponse>> Load(
            CancellationToken token)
        {
            Interlocked.Increment(ref loadCount);
            loadStarted.SetResult(true);

            await releaseLoad.Task.WaitAsync(token);

            return new[] { reading };
        }

        var firstQuery = index.QueryAsync(
            sensorId, Start, Start, Load);

        await loadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var secondQuery = index.QueryAsync(
            sensorId, Start, Start, Load);

        releaseLoad.SetResult(true);

        var results = await Task.WhenAll(
            firstQuery, secondQuery);

        Assert.Equal(1, loadCount);

        foreach (var result in results)
        {
            Assert.Equal(reading.Id, Assert.Single(result).Id);
        }
    }

    private static TelemetryResponse CreateReading(
        Guid sensorId,
        DateTime timestamp)
    {
        return new TelemetryResponse
        {
            Id = Guid.NewGuid(),
            SensorId = sensorId,
            SensorName = "History test sensor",
            FloatValue = 42.5,
            Unit = "%",
            RecordedAtUtc = timestamp
        };
    }
}