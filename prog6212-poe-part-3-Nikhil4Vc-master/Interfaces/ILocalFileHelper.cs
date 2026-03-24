namespace MonthlyClaims_System.Interfaces
{
    public interface ILocalFileHelper
    {
        // Interface used to handle file storage and retrieval from database
        string SaveFile(IFormFile file);

        string GetFilePath(string fileName);

        bool FileExists(string filePath);

        byte[] ReadFile(string filePath);

        Task<string> SaveTempFileAsync(IFormFile file);

        Task DeleteTempFileAsync(string path);
    }
}
