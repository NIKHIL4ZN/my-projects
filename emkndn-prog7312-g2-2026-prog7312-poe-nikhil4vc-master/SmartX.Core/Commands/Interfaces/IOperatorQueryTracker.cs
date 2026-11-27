using SmartX.Core.Commands.DTOs;

namespace SmartX.Core.Commands.Interfaces;

public interface IOperatorQueryTracker
{
    void RecordQuery(string query);

    IReadOnlyList<string> GetRecentQueries();

    IReadOnlyList<QueryPatternResponse>
        GetFrequentPatterns();
}