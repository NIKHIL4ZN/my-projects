using SmartX.Core.Commands.DTOs;

namespace SmartX.Core.Commands.Interfaces;

public interface IPredictiveActionService
{
    Task<IReadOnlyList<
        ActionRecommendationResponse>>
        GetRecommendationsAsync(
            string? operatorQuery = null,
            CancellationToken cancellationToken =
                default);
}