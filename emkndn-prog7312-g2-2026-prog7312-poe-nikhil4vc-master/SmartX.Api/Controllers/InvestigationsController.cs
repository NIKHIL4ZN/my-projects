using Microsoft.AspNetCore.Mvc;
using SmartX.Core.DTOs;
using SmartX.Core.Interfaces;

namespace SmartX.Api.Controllers
{
    [ApiController]
    [Route("api/investigations")]
    public class InvestigationsController : ControllerBase
    {
        private readonly IAnomalyInvestigationService
            _investigationService;

        public InvestigationsController(
            IAnomalyInvestigationService
                investigationService)
        {
            _investigationService =
                investigationService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<
            AnomalyInvestigationResponse>>> GetAll(
            CancellationToken cancellationToken)
        {
            var investigations =
                await _investigationService.AnalyseAsync(
                    cancellationToken);

            return Ok(investigations);
        }
    }
}
