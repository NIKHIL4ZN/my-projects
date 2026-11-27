using Microsoft.AspNetCore.Mvc;
using SmartX.Core.Commands.DTOs;
using SmartX.Core.Commands.Interfaces;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/recommendations")]
public class RecommendationsController
    : ControllerBase
{
    private readonly IPredictiveActionService
        _predictiveActionService;

    private readonly IOperatorQueryTracker
        _queryTracker;

    public RecommendationsController(
        IPredictiveActionService
            predictiveActionService,
        IOperatorQueryTracker queryTracker)
    {
        _predictiveActionService =
            predictiveActionService;

        _queryTracker = queryTracker;
    }

    [HttpGet]
    public async Task<ActionResult<
        IReadOnlyList<
            ActionRecommendationResponse>>>
        GetRecommendations(
            CancellationToken cancellationToken)
    {
        var recommendations =
            await _predictiveActionService
                .GetRecommendationsAsync(
                    cancellationToken:
                        cancellationToken);

        return Ok(recommendations);
    }

    [HttpPost("analyse")]
    public async Task<ActionResult<
        IReadOnlyList<
            ActionRecommendationResponse>>>
        AnalyseOperatorQuery(
            RecommendationQueryRequest request,
            CancellationToken cancellationToken)
    {
        var recommendations =
            await _predictiveActionService
                .GetRecommendationsAsync(
                    request.Query,
                    cancellationToken);

        return Ok(recommendations);
    }

    [HttpGet("query-history")]
    public ActionResult<
        IReadOnlyList<string>>
        GetQueryHistory()
    {
        return Ok(
            _queryTracker.GetRecentQueries());
    }

    [HttpGet("query-patterns")]
    public ActionResult<
        IReadOnlyList<QueryPatternResponse>>
        GetQueryPatterns()
    {
        return Ok(
            _queryTracker.GetFrequentPatterns());
    }
}