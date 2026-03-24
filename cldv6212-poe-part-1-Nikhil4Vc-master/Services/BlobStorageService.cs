using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ARCRetailPOE.Services
{
    public class BlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;

        public BlobStorageService(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("ContainerConnection");
            _containerName = configuration["ContainerName"];
            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(_containerName))
                throw new ArgumentNullException("Configuration for BlobStorageService is missing.");
            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync();

            string uniqueName = Guid.NewGuid().ToString() + Path.GetExtension(fileName);
            var blobClient = containerClient.GetBlobClient(uniqueName);

            await blobClient.UploadAsync(fileStream, overwrite: true);

            return blobClient.Uri.ToString();
        }
    }
}

