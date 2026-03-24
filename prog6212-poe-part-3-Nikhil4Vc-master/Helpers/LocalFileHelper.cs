namespace MonthlyClaims_System.Helpers

{
    using Microsoft.AspNetCore.Hosting;
    using MonthlyClaims_System.Interfaces;
    using System.Threading.Tasks;

    public class LocalFileHelper : ILocalFileHelper
    {
        // Class contains methods used to handle local claims document files
        private readonly string _uploadsFolder;
        private readonly string _tempFolder;

        public LocalFileHelper(IWebHostEnvironment env)
        {
            if (env == null) throw new ArgumentNullException(nameof(env));

            _uploadsFolder = Path.Combine(env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads");
            _tempFolder = Path.Combine(env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "temp");

            if (!Directory.Exists(_uploadsFolder))
                Directory.CreateDirectory(_uploadsFolder);

            if (!Directory.Exists(_tempFolder))
                Directory.CreateDirectory(_tempFolder);
        }

        public string SaveFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is null or empty", nameof(file));

            var storedFileName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName);
            var path = Path.Combine(_uploadsFolder, storedFileName);

            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                file.CopyTo(stream);
            }

            return storedFileName;
        }

        public string GetFilePath(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return string.Empty;

            return Path.Combine(_uploadsFolder, fileName);
        }

        public bool FileExists(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            return System.IO.File.Exists(filePath);
        }

        public byte[] ReadFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentException(nameof(filePath));
            if (!System.IO.File.Exists(filePath)) throw new FileNotFoundException("File not found", filePath);

            return System.IO.File.ReadAllBytes(filePath);
        }

        public async Task<string> SaveTempFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is null or empty", nameof(file));

            var storedFileName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName);
            var path = Path.Combine(_tempFolder, storedFileName);

            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(stream);
            }

            return storedFileName;
        }

        public Task DeleteTempFileAsync(string path)
        {
            if (string.IsNullOrEmpty(path)) return Task.CompletedTask;

            try
            {
                var full = Path.IsPathRooted(path) ? path : Path.Combine(_tempFolder, path);
                if (System.IO.File.Exists(full))
                    System.IO.File.Delete(full);
            }
            catch
            {
            }

            return Task.CompletedTask;
        }
    }
}

