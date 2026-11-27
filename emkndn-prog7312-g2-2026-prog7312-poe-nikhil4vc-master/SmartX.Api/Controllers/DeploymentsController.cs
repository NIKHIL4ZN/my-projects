using Microsoft.AspNetCore.Mvc;
using SmartX.Core.DTOs;
using SmartX.Core.Interfaces;

namespace SmartX.Api.Controllers
{
    [ApiController]
    [Route("api/deployments")]
    public class DeploymentsController : ControllerBase
    {
        private readonly IDeploymentTreeValidator _validator;

        public DeploymentsController(
            IDeploymentTreeValidator validator)
        {
            _validator = validator;
        }

        [HttpPost("validate")]
        public ActionResult<DeploymentValidationResult> Validate(
            DeploymentNodeRequest root)
        {
            var result = _validator.Validate(root);

            if (!result.IsValid)
            {
                return UnprocessableEntity(result);
            }

            return Ok(result);
        }
    }
}
