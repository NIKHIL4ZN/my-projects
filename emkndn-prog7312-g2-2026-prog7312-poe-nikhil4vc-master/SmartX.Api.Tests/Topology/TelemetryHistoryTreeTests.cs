using SmartX.Core.Topology.DataStructures;
using Xunit;

namespace SmartX.Api.Tests.Topology;

public class TelemetryHistoryTreeTests
{
    private static readonly DateTime Start =
        new(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(30, 20, 10)]
    [InlineData(10, 20, 30)]
    [InlineData(30, 10, 20)]
    [InlineData(10, 30, 20)]
    public void Insert_BalancesAllFourRotationCases(
        int first,
        int second,
        int third)
    {
        var tree = new TelemetryHistoryTree<int>();

        tree.Insert(Start.AddMinutes(first), first);
        tree.Insert(Start.AddMinutes(second), second);
        tree.Insert(Start.AddMinutes(third), third);

        Assert.Equal(3, tree.Count);
        Assert.Equal(2, tree.Height);

        Assert.Equal(
            new[] { 10, 20, 30 },
            tree.QueryRange(
                Start,
                Start.AddHours(1)));
    }

    [Fact]
    public void QueryRange_IncludesBoundariesAndReturnsChronologicalValues()
    {
        var tree = new TelemetryHistoryTree<int>();

        foreach (var minute in new[] { 40, 10, 30, 20, 50 })
        {
            tree.Insert(
                Start.AddMinutes(minute),
                minute);
        }

        var results = tree.QueryRange(
            Start.AddMinutes(20),
            Start.AddMinutes(40));

        Assert.Equal(new[] { 20, 30, 40 }, results);
    }

    [Fact]
    public void QueryRange_PreservesReadingsWithIdenticalTimestamps()
    {
        var tree = new TelemetryHistoryTree<string>();

        tree.Insert(Start, "First packet");
        tree.Insert(Start, "Second packet");

        Assert.Equal(2, tree.Count);
        Assert.Equal(1, tree.Height);

        Assert.Equal(
            new[] { "First packet", "Second packet" },
            tree.QueryRange(Start, Start));
    }

    [Fact]
    public void QueryRange_ReturnsEmptyWhenNoTimestampsMatch()
    {
        var tree = new TelemetryHistoryTree<int>();

        tree.Insert(Start, 42);

        var results = tree.QueryRange(
            Start.AddHours(1),
            Start.AddHours(2));

        Assert.Empty(results);
    }

    [Fact]
    public void QueryRange_HandlesAnEmptyTree()
    {
        var tree = new TelemetryHistoryTree<int>();

        Assert.Empty(
            tree.QueryRange(Start, Start.AddHours(1)));

        Assert.Equal(0, tree.Count);
        Assert.Equal(0, tree.Height);
    }

    [Fact]
    public void QueryRange_RejectsReversedBoundaries()
    {
        var tree = new TelemetryHistoryTree<int>();

        Assert.Throws<ArgumentException>(() =>
            tree.QueryRange(
                Start.AddHours(1),
                Start));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Insert_KeepsLargeOrderedHistoryBalanced(
        bool descending)
    {
        const int readingCount = 10_000;

        var tree = new TelemetryHistoryTree<int>();

        for (var index = 0; index < readingCount; index++)
        {
            var value = descending
                ? readingCount - 1 - index
                : index;

            tree.Insert(
                Start.AddSeconds(value),
                value);
        }

        Assert.Equal(readingCount, tree.Count);

        // A plain unbalanced BST would have height 10,000
        // for this input. An AVL tree remains logarithmic.
        Assert.InRange(tree.Height, 1, 20);

        Assert.Equal(
            new[] { 5000, 5001, 5002, 5003, 5004, 5005 },
            tree.QueryRange(
                Start.AddSeconds(5000),
                Start.AddSeconds(5005)));
    }
}