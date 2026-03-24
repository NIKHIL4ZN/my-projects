using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonthlyClaims_System.Data;
using MonthlyClaims_System.Filters;
using System.Text;

namespace MonthlyClaims_System.Controllers
{
    [Authorize(Roles = "Coordinator")]
    [SessionRoleFilter("Coordinator")]
    public class CoordinatorController : Controller
    {
        private readonly ClaimsDbContext _context;

        private readonly ILogger<CoordinatorController> _logger;

        public CoordinatorController(ClaimsDbContext context, ILogger<CoordinatorController> logger)
        {
            _context = context;
            _logger = logger;
        }

        //Method that allows for the download of claims summary Csv file document
        public IActionResult DownloadCoordinatorReport()
        {
            var claims = _context.Claims
                .Where(c => c.Status == "Pending" || c.Status == "Verified")
                .ToList();

            var csv = new StringBuilder();
            csv.AppendLine("ClaimId,LecturerName,Date,TotalAmount,Status");

            foreach (var claim in claims)
            {
                csv.AppendLine($"{claim.ClaimId},{claim.LecturerName},{claim.ClaimDate:yyyy-MM-dd},{claim.TotalAmount},{claim.Status}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", "CoordinatorClaimsReport.csv");
        }

        // Metod that returns claims
        public IActionResult VerifyClaims()
        {
            var claims = _context.Claims
                .Where(c => c.Status == "Pending")
                .OrderByDescending(c => c.ClaimDate)
                .ToList();

            return View(claims);
        }

        // Method used to Verify claims that will be sent to the Manager view
        [HttpPost]
        public IActionResult Verify(int id)
        {
            _logger.LogInformation("Coordinator {User} is attempting to verify claim #{ClaimId}.",
                User.Identity?.Name, id);

            try
            {
                var claim = _context.Claims.FirstOrDefault(c => c.ClaimId == id);

                if (claim == null)
                {
                    _logger.LogWarning("Coordinator {User} attempted to verify claim #{ClaimId}, but it was not found.",
                        User.Identity?.Name, id);

                    TempData["Error"] = "Claim not found.";
                    return RedirectToAction("VerifyClaims");
                }

                claim.Status = "Verified";
                _context.SaveChanges();

                _logger.LogInformation("Claim #{ClaimId} successfully verified by coordinator {User}.",
                    id, User.Identity?.Name);

                TempData["Success"] = $"Claim #{id} verified successfully.";
                return RedirectToAction("VerifyClaims");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Coordinator {User} encountered an error verifying claim #{ClaimId}.",
                    User.Identity?.Name, id);

                TempData["Error"] = "An error occurred during verification.";
                return RedirectToAction("VerifyClaims");
            }
        }

        // Method to reject claims
        [HttpPost]
        public IActionResult Reject(int id)
        {
            _logger.LogInformation("Coordinator {User} is rejecting claim #{ClaimId}.",
                User.Identity?.Name, id);

            try
            {
                var claim = _context.Claims.FirstOrDefault(c => c.ClaimId == id);

                if (claim == null)
                {
                    _logger.LogWarning("Coordinator {User} attempted to reject claim #{ClaimId}, but it was not found.",
                        User.Identity?.Name, id);

                    TempData["Error"] = "Claim not found.";
                    return RedirectToAction("VerifyClaims");
                }

                claim.Status = "Rejected";
                _context.SaveChanges();

                _logger.LogInformation("Claim #{ClaimId} successfully rejected by coordinator {User}.",
                    id, User.Identity?.Name);

                TempData["Error"] = $"Claim #{id} rejected.";
                return RedirectToAction("VerifyClaims");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Coordinator {User} encountered an error rejecting claim #{ClaimId}.",
                    User.Identity?.Name, id);

                TempData["Error"] = "An error occurred while rejecting the claim.";
                return RedirectToAction("VerifyClaims");
            }
        }


    }
}
