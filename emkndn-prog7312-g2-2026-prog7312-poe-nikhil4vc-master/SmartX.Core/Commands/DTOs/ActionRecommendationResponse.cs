using SmartX.Core.Commands.Enums;

namespace SmartX.Core.Commands.DTOs;

public class ActionRecommendationResponse
{
    public Guid RecommendationId { get; init; }

    public Guid SensorId { get; init; }

    public string SensorName { get; init; } =
        string.Empty;

    public string Title { get; init; } =
        string.Empty;

    public string Explanation { get; init; } =
        string.Empty;

    public DeviceCommandType RecommendedCommandType
    {
        get;
        init;
    }

    public CommandPriority Priority { get; init; }

    public double ConfidencePercent { get; init; }

    public string SuggestedPayload { get; init; } =
        string.Empty;

    public IReadOnlyList<string> Evidence
    {
        get;
        init;
    } = [];
}