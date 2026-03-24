using Microsoft.AspNetCore.Mvc;

namespace MonthlyClaims_System.Controllers
{
    public class ErrorController : Controller
    {
        // Controller used for error handling
        [Route("Error/Handle/{code}")]
        public IActionResult Handle(int code)
        {
            if (code == 404) return View("Error404");
            if (code == 403) return View("Error403");

            return View("Error500");
        }

        [Route("Error/ServerError")]
        public IActionResult ServerError()
        {
            return View("Error500");
        }
    }
}
