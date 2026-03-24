using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MonthlyClaims_System.Data;
using MonthlyClaims_System.Filters;
using MonthlyClaims_System.Models;
using System.Text;

namespace MonthlyClaims_System.Controllers
{
    [Authorize(Roles = "HR")]
    [SessionRoleFilter("HR")]
    public class HRController : Controller
    {
        private readonly ClaimsDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public HRController(
            ClaimsDbContext context,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult HRDashboard() => View();

        // Method to return new rate value set by HR user
        public IActionResult ManageRate()
        {
            var rate = _context.ClaimRates.First();
            return View(rate);
        }

        // Method allows HR user to change claims rate value
        [HttpPost]
        public IActionResult ManageRate(ClaimRate model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var rate = _context.ClaimRates.First();
            rate.HourlyRate = model.HourlyRate;

            _context.SaveChanges();

            TempData["Success"] = "Hourly rate updated successfully!";
            return RedirectToAction("ManageRate");
        }

        // Method returns system user details
        public IActionResult ManageUsers()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

        // Methods returns newely created users
        [HttpGet]
        public IActionResult CreateUser()
        {
            ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).ToList();
            return View();
        }

        // Method allows HR user to create new user accounts into the system
        [HttpPost]
        public async Task<IActionResult> CreateUser(string email, string password, string role)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Email and password are required.";
                return RedirectToAction("CreateUser");
            }

            var exists = await _userManager.FindByEmailAsync(email);
            if (exists != null)
            {
                TempData["Error"] = "A user with this email already exists.";
                return RedirectToAction("CreateUser");
            }

            var user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join("<br>", result.Errors.Select(e => e.Description));
                return RedirectToAction("CreateUser");
            }

            await _userManager.AddToRoleAsync(user, role);

            TempData["Success"] = "User created successfully!";
            return RedirectToAction("ManageUsers");
        }

        // Method that returns current account details of other users
        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var model = new EditUserViewModel
            {
                UserId = user.Id,
                Email = user.Email,
                SelectedRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault()
            };

            ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).ToList();
            return View(model);
        }

        // Method allows HR user to edit account details of other users in the system
        [HttpPost]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            user.Email = model.Email;
            user.UserName = model.Email;

            await _userManager.UpdateAsync(user);

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            await _userManager.AddToRoleAsync(user, model.SelectedRole);

            TempData["Success"] = "User updated successfully!";
            return RedirectToAction("ManageUsers");
        }

        // Method returns view after deletion of user
        [HttpGet]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("ManageUsers");
            }

            return View(user); 
        }

        // Method that allows HR user to delete other users accounts
        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id, IFormCollection form)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("ManageUsers");
            }

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                TempData["Success"] = "User deleted successfully.";
            }
            else
            {
                TempData["Error"] =
                    string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction("ManageUsers");
        }


        // Method that allows HR user to download a Csv file that includes the summary of claims
        [HttpGet]
        public IActionResult ExportCsv()
        {
            var claims = _context.Claims.ToList();

            var csv = new StringBuilder();

            csv.AppendLine("ClaimId,LecturerName,ClaimDate,HoursWorked,HourlyRate,TotalAmount,Status");

            foreach (var c in claims)
            {
                csv.AppendLine($"{c.ClaimId},{c.LecturerName},{c.ClaimDate:yyyy-MM-dd},{c.HoursWorked},{c.HourlyRate},{c.TotalAmount},{c.Status}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", "ClaimsReport.csv");
        }

        // Method used to filter claims from a specific criteria as HR user requests
        public IActionResult ClaimsReport(
           string status,
           string lecturer,
           decimal? minAmount,
           decimal? maxAmount,
           decimal? minRate,
           decimal? maxRate,
           DateTime? fromDate,
           DateTime? toDate)
        {
            var claims = _context.Claims.AsQueryable();

            if (!string.IsNullOrEmpty(status))
                claims = claims.Where(c => c.Status == status);

            if (!string.IsNullOrEmpty(lecturer))
                claims = claims.Where(c => c.LecturerName.Contains(lecturer));

            if (minAmount.HasValue)
                claims = claims.Where(c => c.TotalAmount >= minAmount.Value);

            if (maxAmount.HasValue)
                claims = claims.Where(c => c.TotalAmount <= maxAmount.Value);

            if (minRate.HasValue)
                claims = claims.Where(c => c.HourlyRate >= minRate.Value);

            if (maxRate.HasValue)
                claims = claims.Where(c => c.HourlyRate <= maxRate.Value);

            if (fromDate.HasValue)
                claims = claims.Where(c => c.ClaimDate >= fromDate.Value);

            if (toDate.HasValue)
                claims = claims.Where(c => c.ClaimDate <= toDate.Value);

            return View(claims.ToList());
        }
    }
}

