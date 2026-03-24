using ARCRetailPOE.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;


namespace ARCRetailPOE.Controllers
{
    [Authorize(Roles = "Admin")] 
    public class ContractController : Controller
    {
        private readonly FileStorageService _fileService;

        public ContractController(FileStorageService fileService)
        {
            _fileService = fileService;
        }

        public async Task<IActionResult> Index()
        {
            var files = await _fileService.GetAllFilesAsync();
            return View(files);
        }

        public IActionResult Upload() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile contractFile)
        {
            if (contractFile != null && contractFile.Length > 0)
            {
                using var stream = contractFile.OpenReadStream();
                await _fileService.UploadContractAsync(contractFile.FileName, stream);
                TempData["Message"] = $"✅ Contract '{contractFile.FileName}' uploaded successfully.";
                return RedirectToAction("Index");
            }

            TempData["ErrorMessage"] = "⚠️ Please select a file to upload.";
            return View();
        }

        public async Task<IActionResult> Download(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return NotFound();

            var stream = await _fileService.DownloadContractAsync(fileName);
            if (stream == null)
                return NotFound();

            return File(stream, "application/octet-stream", fileName);
        }
    }
}
