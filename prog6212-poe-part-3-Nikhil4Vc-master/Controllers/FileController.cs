using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonthlyClaims_System.Helpers;
using MonthlyClaims_System.Interfaces;

namespace MonthlyClaims_System.Controllers
{
    [Authorize(Roles = "Lecturer,Coordinator,Manager,HR")]
    public class FileController : Controller
    {
        // Controller used for file handling between all users of the system

        private readonly ILocalFileHelper _fileHelper;
        private readonly ILogger<FileController> _logger;

        public FileController(ILogger<FileController> logger, ILocalFileHelper fileHelper)
        {
            _logger = logger;
            _fileHelper = fileHelper;
        }

        // Downlaod claims file method for all users of the system
        public IActionResult Download(string blobFileName, string originalFileName)
        {
            if (string.IsNullOrEmpty(blobFileName))
                return NotFound();

            string filePath = _fileHelper.GetFilePath(blobFileName);

            if (!_fileHelper.FileExists(filePath))
                return NotFound();

            var bytes = _fileHelper.ReadFile(filePath);

            return File(bytes, "application/octet-stream", originalFileName);
        }
    }
}

