using SmartX.Core.Commands.DTOs;
using SmartX.Core.Commands.Interfaces;

namespace SmartX.Infrastructure.Commands;

public class OperatorQueryTracker
    : IOperatorQueryTracker
{
    private const int MaximumHistorySize = 20;

    private readonly object _syncRoot = new();

    // FIFO history of the operator's recent searches.
    private readonly Queue<string>
        _recentQueries = new();

    // O(1) complexityy keyword-frequency lookup.
    private readonly Dictionary<string, int>
        _keywordFrequency =
            new(StringComparer.OrdinalIgnoreCase);

    // Remove common words that do not describe an operational pattern.
    private readonly HashSet<string>
        _ignoredWords =
            new(StringComparer.OrdinalIgnoreCase)
            {
               "a",
            "an",
            "and",
            "are",
            "check",
            "for",
            "from",
            "in",
            "investigate",
            "is",
            "my",
            "of",
            "on",
            "the",
            "to",
            "with"
            };

    public void RecordQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return;
        }

        var normalisedQuery = query.Trim();

        lock (_syncRoot)
        {
            _recentQueries.Enqueue(
                normalisedQuery);

            while (_recentQueries.Count >
                   MaximumHistorySize)
            {
                _recentQueries.Dequeue();
            }

            foreach (var keyword
                     in ExtractKeywords(
                         normalisedQuery))
            {
                _keywordFrequency.TryGetValue(
                    keyword,
                    out var currentCount);

                _keywordFrequency[keyword] =
                    currentCount + 1;
            }
        }
    }

    public IReadOnlyList<string>
        GetRecentQueries()
    {
        lock (_syncRoot)
        {
            return _recentQueries
                .Reverse()
                .ToList();
        }
    }

    public IReadOnlyList<QueryPatternResponse>
        GetFrequentPatterns()
    {
        lock (_syncRoot)
        {
            return _keywordFrequency
                .OrderByDescending(
                    item => item.Value)
                .ThenBy(
                    item => item.Key)
                .Take(10)
                .Select(item =>
                    new QueryPatternResponse
                    {
                        Keyword = item.Key,
                        SearchCount = item.Value
                    })
                .ToList();
        }
    }

    private IEnumerable<string>
        ExtractKeywords(string query)
    {
        var separators =
            new[]
            {
                ' ',
                ',',
                '.',
                ':',
                ';',
                '?',
                '!',
                '/',
                '\\'
            };

        return query
            .Split(
                separators,
                StringSplitOptions
                    .RemoveEmptyEntries |
                StringSplitOptions
                    .TrimEntries)
            .Select(
                word =>
                    word.ToLowerInvariant())
            .Where(
                word =>
                    word.Length >= 3 &&
                    !_ignoredWords.Contains(word))
            .Distinct(
                StringComparer.OrdinalIgnoreCase);
    }
}