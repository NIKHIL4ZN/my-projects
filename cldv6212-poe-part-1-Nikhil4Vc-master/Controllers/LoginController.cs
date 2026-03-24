using ARCRetailPOE.Models;
using ARCRetailPOE.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using BCryptNet = BCrypt.Net.BCrypt;



namespace ARCRetailPOE.Controllers
{
    public class LoginController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly LoginService _loginService;

        public LoginController(LoginService loginService, IConfiguration configuration)
        {
            _loginService = loginService;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult GenerateHash(string password = "123")
        {
            string hash = BCrypt.Net.BCrypt.HashPassword(password);
            return Content(hash);
        }


        public IActionResult Index() => View();

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model.Password != model.ConfirmPassword)
            {
                ViewBag.ErrorMessage = "Passwords do not match.";
                return View(model);
            }

            string passwordHash = BCryptNet.HashPassword(model.Password);

            try
            {
                string connectionString = _configuration.GetConnectionString("AzureSQLConnection");

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string checkQuery = "SELECT COUNT(*) FROM CustomerProfiles WHERE Email = @Email";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Email", model.Email);
                        int count = (int)await checkCmd.ExecuteScalarAsync();
                        if (count > 0)
                        {
                            ViewBag.ErrorMessage = "An account with this email already exists.";
                            return View(model);
                        }
                    }

                    string insertQuery = @"
                        INSERT INTO CustomerProfiles (FullName, Email, PasswordHash, Role)
                        VALUES (@FullName, @Email, @PasswordHash, @Role)";

                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@FullName", model.FullName);
                        cmd.Parameters.AddWithValue("@Email", model.Email);
                        cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                        cmd.Parameters.AddWithValue("@Role", "Customer");
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                TempData["Message"] = "✅ Registration successful. You can now log in.";
                return RedirectToAction("Index", "Login");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"⚠️ Error during registration: {ex.Message}";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Index", "Login");
        }

        [HttpPost]
        public async Task<IActionResult> Index(string email, string password)
        {
            var (success, role) = await _loginService.AuthenticateUserAsync(email, password);

            if (!success)
            {
                ViewBag.Error = "Invalid credentials.";
                return View();
            }

            var claims = new List<Claim>
{
    new Claim(ClaimTypes.Name, email),
    new Claim(ClaimTypes.Role, role)
};

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            if (role == "Admin")
            {
                return RedirectToAction("Index", "Customer"); 
            }
            else
            {
                return RedirectToAction("Index", "Product"); 
            }
        }
    }
}

