using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace DbMigration.Common.Legacy.ClientStorage.Clients
{
    public class BlobStorageClient
    {
        private readonly BlobServiceClient _blobServiceClient;

        public BlobStorageClient(StorageClient storageClient)
        {
            //Guide: https://learn.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet?tabs=visual-studio%2Cmanaged-identity%2Croles-azure-portal%2Csign-in-azure-cli%2Cidentity-visual-studio

            if (storageClient.StorageConnectionString == "UseDevelopmentStorage=true")
            {
                //Expected BlobEndpoint format: "http://127.0.0.1:10000/devstoreaccount1"
                _blobServiceClient = new BlobServiceClient(
                    new Uri(storageClient.CloudStorageAccount.BlobEndpoint.ToString()),
                    new StorageSharedKeyCredential("devstoreaccount1", "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw=="));
            }
            else
            {
                //Expected BlobEndpoint format: https://dbmigratedev.blob.core.windows.net/
                _blobServiceClient = new BlobServiceClient(
                    new Uri(storageClient.CloudStorageAccount.BlobEndpoint.ToString()),
                    new DefaultAzureCredential());

            }

        }

        public BlobContainerClient GetContainerClient(string blobContainerName)
        {
            return _blobServiceClient.GetBlobContainerClient(blobContainerName);
        }

        public BlobClient GetBlobClient(string blobContainerName, string blobName)
        {
            var blobContainerClient = _blobServiceClient.GetBlobContainerClient(blobContainerName);
            var blobClient = blobContainerClient.GetBlobClient(blobName);
            return blobClient;
        }

        /// <summary>
        /// Creates a container if it does not exist.
        /// </summary>
        /// <returns></returns>
        public async Task<BlobContainerClient> CreateContainer(string containerName, PublicAccessType publicAccessType = PublicAccessType.None, CancellationToken cancellationToken = default)
        {
            var blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await blobContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            return blobContainerClient;
        }

    }
}
