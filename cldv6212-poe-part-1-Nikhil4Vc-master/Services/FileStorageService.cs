using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ARCRetailPOE.Services
{
    public class FileStorageService
    {
        private readonly ShareClient _shareClient;

        public FileStorageService(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("ContainerConnection");
            var shareName = configuration["FileShareName"];

            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(shareName))
                throw new ArgumentNullException("File share configuration is missing.");

            _shareClient = new ShareClient(connectionString, shareName);
            _shareClient.CreateIfNotExists();
        }

        public async Task UploadContractAsync(string fileName, Stream fileStream)
        {
            var rootDir = _shareClient.GetRootDirectoryClient();
            var fileClient = rootDir.GetFileClient(fileName);

            await fileClient.CreateAsync(fileStream.Length);
            await fileClient.UploadRangeAsync(
                new Azure.HttpRange(0, fileStream.Length),
                fileStream
            );
        }

        public async Task<List<string>> GetAllFilesAsync()
        {
            var fileNames = new List<string>();
            var rootDir = _shareClient.GetRootDirectoryClient();

            await foreach (var item in rootDir.GetFilesAndDirectoriesAsync())
            {
                if (!item.IsDirectory)
                    fileNames.Add(item.Name);
            }

            return fileNames;
        }

        public async Task<Stream> DownloadContractAsync(string fileName)
        {
            var rootDir = _shareClient.GetRootDirectoryClient();
            var fileClient = rootDir.GetFileClient(fileName);

            if (await fileClient.ExistsAsync())
            {
                var download = await fileClient.DownloadAsync();
                return download.Value.Content;
            }

            return null;
        }
    }
}

