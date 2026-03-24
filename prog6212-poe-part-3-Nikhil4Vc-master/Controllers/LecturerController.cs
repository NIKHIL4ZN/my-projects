using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonthlyClaims_System.Data;
using MonthlyClaims_System.Filters;
using MonthlyClaims_System.Helpers;
using MonthlyClaims_System.Models;
using System.Text;
using MonthlyClaims_System.Interfaces;


namespace MonthlyClaims_System.Controllers
{
    [Authorize(Roles = "Lecturer")]
    [SessionRoleFilter("Lecturer")]
    public class LecturerController : Controller
    {

        private readonly ClaimsDbContext _context;
        private readonly ILogger<LecturerController> _logger;
        private readonly ILocalFileHelper _localFileHelper; 

        public LecturerController(
            ClaimsDbContext context,
            ILogger<LecturerController> logger,
            ILocalFileHelper localFileHelper) 
        {
            _context = context;
            _logger = logger;
            _localFileHelper = localFileHelper;
        }

        // Method that allows Lecturer users to download submitted claims document
        public IActionResult DownloadReport(string status = "")
        {
            var userEmail = User.Identity?.Name;

            var claims = _context.Claims
                .Where(c => c.LecturerEmail == userEmail)
                .ToList();

            if (!string.IsNullOrEmpty(status))
                claims = claims.Where(c => c.Status == status).ToList();

            var csv = new StringBuilder();
            csv.AppendLine("ClaimId,Date,HoursWorked,HourlyRate,TotalAmount,Status");

            foreach (var claim in claims)
            {
                csv.AppendLine($"{claim.ClaimId},{claim.ClaimDate:yyyy-MM-dd},{claim.HoursWorked},{claim.HourlyRate},{claim.TotalAmount},{claim.Status}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", "MyClaimsReport.csv");
        }

        // Method that allwos for the submission of claims information for a claim submission by the Lecturer user
        [HttpPost]
        public async Task<IActionResult> SubmitClaim(Claim claim, IFormFile document)
        {
            _logger.LogInformation("Lecturer {User} is submitting a claim.", User.Identity?.Name);

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please correct the errors and try again.";
                return View("SubmitClaim", claim);
            }

            var hrRate = _context.ClaimRates.First().HourlyRate;
            claim.HourlyRate = hrRate;
            claim.TotalAmount = claim.HoursWorked * hrRate;
            claim.Status = "Pending";

            if (document != null && document.Length > 0)
            {
                try
                {
                    string storedFileName = _localFileHelper.SaveFile(document);

                    claim.BlobFileName = storedFileName;        
                    claim.OriginalFileName = document.FileName; 
                    claim.DocumentPath = storedFileName;        
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Local file upload failed for {User}.", User.Identity?.Name);
                    TempData["Error"] = "File upload failed.";
                    return View("SubmitClaim", claim);
                }
            }

            claim.LecturerEmail = User.Identity?.Name;
            claim.LecturerName = claim.LecturerEmail.Split('@')[0];

            _context.Claims.Add(claim);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Claim submitted successfully!";
            return RedirectToAction("SubmitClaim");
        }

        // Returns view after claim submission
        [HttpGet]
        public IActionResult SubmitClaim()
        {
            ViewBag.LoggedInUser = HttpContext.Session.GetString("UserEmail");
            return View(new Claim());
        }

        // Method allows Lecturer users to view claims history details of past claims
        public IActionResult ViewClaims(string? status)
        {
            var claims = _context.Claims.AsQueryable();

            if (!string.IsNullOrEmpty(status))
                claims = claims.Where(c => c.Status == status);

            return View(claims.ToList());
        }

        // Method allows for the retrieval of claims document from the database and allows the document to be downloaded by Lecturer user
        public IActionResult DownloadFile(string blobFileName, string originalFileName)
        {
            _logger.LogInformation("User {User} requested download of file {BlobFileName}.",
                User.Identity?.Name, blobFileName);

            if (string.IsNullOrEmpty(blobFileName))
                return NotFound("Invalid file name.");

            string filePath = _localFileHelper.GetFilePath(blobFileName);

            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogError("File not found on server for {BlobFileName}", blobFileName);
                return NotFound("File not found.");
            }

            var fileBytes = System.IO.File.ReadAllBytes(filePath);

            _logger.LogInformation("File {OriginalFileName} successfully downloaded by {User}.",
                    originalFileName, User.Identity?.Name);

            return File(fileBytes, "application/octet-stream", originalFileName);
        }
    }
}
