using SmartX.Infrastructure.Commands;

namespace SmartX.Api.Tests.Commands;

public class OperatorQueryTrackerTests
{
    [Fact]
    public void RecordQuery_StoresRecentQueriesNewestFirst()
    {
        var tracker = new OperatorQueryTracker();

        tracker.RecordQuery(
            "Check gateway connection");

        tracker.RecordQuery(
            "Inspect power usage");

        tracker.RecordQuery(
            "Review sensor calibration");

        var queries =
            tracker.GetRecentQueries();

        Assert.Equal(3, queries.Count);
        Assert.Equal(
            "Review sensor calibration",
            queries[0]);
        Assert.Equal(
            "Inspect power usage",
            queries[1]);
        Assert.Equal(
            "Check gateway connection",
            queries[2]);
    }

    [Fact]
    public void RecordQuery_IgnoresNullEmptyAndWhitespaceQueries()
    {
        var tracker = new OperatorQueryTracker();

        tracker.RecordQuery(null!);
        tracker.RecordQuery(string.Empty);
        tracker.RecordQuery("   ");

        var queries =
            tracker.GetRecentQueries();

        Assert.Empty(queries);
        Assert.Empty(
            tracker.GetFrequentPatterns());
    }

    [Fact]
    public void GetRecentQueries_RetainsOnlyTwentyQueries()
    {
        var tracker = new OperatorQueryTracker();

        for (var index = 1;
             index <= 25;
             index++)
        {
            tracker.RecordQuery(
                $"Sensor query {index}");
        }

        var queries =
            tracker.GetRecentQueries();

        Assert.Equal(20, queries.Count);
        Assert.Equal(
            "Sensor query 25",
            queries[0]);
        Assert.Equal(
            "Sensor query 6",
            queries[^1]);

        Assert.DoesNotContain(
            "Sensor query 5",
            queries);
    }

    [Fact]
    public void GetFrequentPatterns_CountsRepeatedKeywords()
    {
        var tracker = new OperatorQueryTracker();

        tracker.RecordQuery(
            "Review power usage");

        tracker.RecordQuery(
            "Power spike detected");

        tracker.RecordQuery(
            "Inspect power meter");

        var patterns =
            tracker.GetFrequentPatterns();

        var powerPattern =
            Assert.Single(
                patterns,
                pattern =>
                    pattern.Keyword == "power");

        Assert.Equal(
            3,
            powerPattern.SearchCount);
    }

    [Fact]
    public void GetFrequentPatterns_IgnoresCommonWords()
    {
        var tracker = new OperatorQueryTracker();

        tracker.RecordQuery(
            "Check the power sensor");

        tracker.RecordQuery(
            "Investigate the power meter");

        var patterns =
            tracker.GetFrequentPatterns();

        Assert.DoesNotContain(
            patterns,
            pattern =>
                pattern.Keyword == "check");

        Assert.DoesNotContain(
            patterns,
            pattern =>
                pattern.Keyword ==
                "investigate");

        Assert.DoesNotContain(
            patterns,
            pattern =>
                pattern.Keyword == "the");

        var powerPattern =
            Assert.Single(
                patterns,
                pattern =>
                    pattern.Keyword == "power");

        Assert.Equal(
            2,
            powerPattern.SearchCount);
    }

    [Fact]
    public void RecordQuery_CountsKeywordOncePerQuery()
    {
        var tracker = new OperatorQueryTracker();

        tracker.RecordQuery(
            "Power power POWER meter");

        var pattern =
            Assert.Single(
                tracker.GetFrequentPatterns(),
                item =>
                    item.Keyword == "power");

        Assert.Equal(
            1,
            pattern.SearchCount);
    }

    [Fact]
    public void GetFrequentPatterns_IsCaseInsensitive()
    {
        var tracker = new OperatorQueryTracker();

        tracker.RecordQuery(
            "POWER usage");

        tracker.RecordQuery(
            "Power meter");

        tracker.RecordQuery(
            "power spike");

        var pattern =
            Assert.Single(
                tracker.GetFrequentPatterns(),
                item =>
                    item.Keyword == "power");

        Assert.Equal(
            3,
            pattern.SearchCount);
    }

    [Fact]
    public void GetFrequentPatterns_OrdersByFrequencyThenAlphabetically()
    {
        var tracker = new OperatorQueryTracker();

        tracker.RecordQuery(
            "temperature gateway");

        tracker.RecordQuery(
            "gateway temperature");

        tracker.RecordQuery(
            "moisture");

        var patterns =
            tracker.GetFrequentPatterns();

        Assert.Equal(
            "gateway",
            patterns[0].Keyword);
        Assert.Equal(
            2,
            patterns[0].SearchCount);

        Assert.Equal(
            "temperature",
            patterns[1].Keyword);
        Assert.Equal(
            2,
            patterns[1].SearchCount);
    }

    [Fact]
    public void GetFrequentPatterns_ReturnsAtMostTenPatterns()
    {
        var tracker = new OperatorQueryTracker();

        tracker.RecordQuery(
            "alpha bravo charlie delta echo " +
            "foxtrot golf hotel india juliet " +
            "kilo lima mike");

        var patterns =
            tracker.GetFrequentPatterns();

        Assert.Equal(10, patterns.Count);
    }
}