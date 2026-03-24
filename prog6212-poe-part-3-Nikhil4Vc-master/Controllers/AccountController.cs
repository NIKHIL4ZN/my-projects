using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MonthlyClaims_System.Controllers
{
    public class AccountController : Controller
    {

        // Controller used to handle role loggin using Identity Scaffolding

        public IActionResult Login()
        {
            return View();
        }

        private readonly SignInManager<IdentityUser> _signInManager;

        public AccountController(SignInManager<IdentityUser> signInManager)
        {
            _signInManager = signInManager;
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();

            return Redirect("/Identity/Account/Login");
        }


    }
}
