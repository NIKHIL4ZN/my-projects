using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonthlyClaims_System.Data;
using MonthlyClaims_System.Filters;
using System.Text;

namespace MonthlyClaims_System.Controllers
{
    [Authorize(Roles = "Manager")]
    [SessionRoleFilter("Manager")]
    public class ManagerController : Controller
    {
        private readonly ClaimsDbContext _context;

        private readonly ILogger<ManagerController> _logger;

        public ManagerController(ClaimsDbContext context, ILogger<ManagerController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Method allows for Manager user to download Csv summary document of claims
        public IActionResult DownloadManagerReport()
        {
            var claims = _context.Claims
                .Where(c => c.Status == "Approved" || c.Status == "Rejected")
                .ToList();

            var csv = new StringBuilder();
            csv.AppendLine("ClaimId,LecturerName,Date,TotalAmount,Status");

            foreach (var claim in claims)
            {
                csv.AppendLine($"{claim.ClaimId},{claim.LecturerName},{claim.ClaimDate:yyyy-MM-dd},{claim.TotalAmount},{claim.Status}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", "ManagerClaimsReport.csv");
        }


        // Returns approved claims
        public IActionResult ApproveClaims()
        {
            var claims = _context.Claims
                .Where(c => c.Status == "Verified")
                .ToList();

            return View(claims);
        }

        // Method allows claims to be approved by the Manager user
        [HttpPost]
        public IActionResult ApproveClaim(int id)
        {
            _logger.LogInformation("Manager {User} is attempting to APPROVE claim #{ClaimId}.",
                User.Identity?.Name, id);

            try
            {
                var claim = _context.Claims.FirstOrDefault(c => c.ClaimId == id);

                if (claim == null)
                {
                    _logger.LogWarning("Manager {User} attempted to approve claim #{ClaimId}, but it was NOT FOUND.",
                        User.Identity?.Name, id);

                    TempData["Error"] = "Claim not found.";
                    return RedirectToAction("ApproveClaims");
                }

                claim.Status = "Approved";
                _context.SaveChanges();

                _logger.LogInformation("Manager {User} SUCCESSFULLY approved claim #{ClaimId}.",
                    User.Identity?.Name, id);

                TempData["Success"] = $"Claim #{id} approved successfully!";
                return RedirectToAction("ApproveClaims");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Manager {User} encountered an ERROR while approving claim #{ClaimId}.",
                    User.Identity?.Name, id);

                TempData["Error"] = "An error occurred while approving the claim.";
                return RedirectToAction("ApproveClaims");
            }
        }

        // Method allows claims to be rejected by the Manager user
        [HttpPost]
        public IActionResult RejectClaim(int id)
        {
            _logger.LogInformation("Manager {User} is attempting to REJECT claim #{ClaimId}.",
                User.Identity?.Name, id);

            try
            {
                var claim = _context.Claims.FirstOrDefault(c => c.ClaimId == id);

                if (claim == null)
                {
                    _logger.LogWarning("Manager {User} attempted to reject claim #{ClaimId}, but it was NOT FOUND.",
                        User.Identity?.Name, id);

                    TempData["Error"] = "Claim not found.";
                    return RedirectToAction("ApproveClaims");
                }

                claim.Status = "Rejected";
                _context.SaveChanges();

                _logger.LogInformation("Manager {User} SUCCESSFULLY rejected claim #{ClaimId}.",
                    User.Identity?.Name, id);

                TempData["Error"] = $"Claim #{id} has been rejected.";
                return RedirectToAction("ApproveClaims");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Manager {User} encountered an ERROR while rejecting claim #{ClaimId}.",
                    User.Identity?.Name, id);

                TempData["Error"] = "An error occurred while rejecting the claim.";
                return RedirectToAction("ApproveClaims");
            }
        }

    }
}
