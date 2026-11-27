namespace SmartX.Core.Commands.DTOs;

public class QueryPatternResponse
{
    public string Keyword { get; init; } =
        string.Empty;

    public int SearchCount { get; init; }
}