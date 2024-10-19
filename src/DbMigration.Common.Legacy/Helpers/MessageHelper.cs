using Azure.Storage.Blobs;
using DbMigration.Common.Legacy.ClientStorage.Clients;
using DbMigration.Common.Legacy.Messaging;
using Microsoft.Extensions.Logging;

namespace DbMigration.Common.Legacy.Helpers
{
    public class MessageHelper
    {
        private readonly StorageClient _internalStorageClient;
        private readonly ILogger<MessageHelper> _log;
        private BlobStorageClient _blobStorageClient;


        public MessageHelper(StorageClient internalStorageClient, ILogger<MessageHelper> log)
        {
            _internalStorageClient = internalStorageClient;
            _log = log;
        }

        private BlobStorageClient InternalBlobStorageClient
        {
            get
            {
                if (_blobStorageClient != null)
                {
                    return _blobStorageClient;
                }
                else
                {
                    _blobStorageClient = _internalStorageClient.GetBlobStorageClient();
                    return _blobStorageClient;
                }

            }
        }

        public async Task<MessageBase<T>> BuildMessage<T>(MessageType messageType, string fileJsonContent, T data, string blobContainerName = "messageblobs") where T : IMessageData
        {
            Guid messageId = Guid.NewGuid();
            string blobUri = string.Empty;

            if (!string.IsNullOrEmpty(fileJsonContent))
            {
                BlobContainerClient containerClient = InternalBlobStorageClient.GetContainerClient(blobContainerName);
                await containerClient.CreateIfNotExistsAsync();

                var blobClient = containerClient.GetBlobClient($"{messageId}.json");

                byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(fileJsonContent);
                BinaryData bn = new BinaryData(byteArray);
                _ = blobClient.UploadAsync(bn, overwrite: true, CancellationToken.None).Result;
                blobUri = blobClient.Uri.ToString();
            }

            return new MessageBase<T>
            {
                Data = data,
                MessageType = messageType,
                MessageId = messageId,
                MessageFilePath = blobUri

            };
        }

        public async Task<string> GetMessageFileContent<T>(MessageBase<T> message) where T : IMessageData
        {
            BlobUriBuilder blobUriBuilder = new BlobUriBuilder(new Uri(message.MessageFilePath));
            string containerName = blobUriBuilder.BlobContainerName;

            BlobContainerClient containerClient = InternalBlobStorageClient.GetContainerClient(containerName);

            BlobClient blobClient = containerClient.GetBlobClient(blobUriBuilder.BlobName);

            var content = await blobClient.DownloadContentAsync();

            string rowsJson = content.Value.Content.ToString();

            return rowsJson;
        }

        public async Task CleanupMessageFile<T>(MessageBase<T> message) where T : IMessageData
        {
            if (message.DeleteAfterProcessing && !string.IsNullOrEmpty(message.MessageFilePath))
            {
                BlobUriBuilder blobUriBuilder = new BlobUriBuilder(new Uri(message.MessageFilePath));
                string containerName = blobUriBuilder.BlobContainerName;
                string blobName = blobUriBuilder.BlobName;

                _log.LogInformation($"Deleting blob {blobName} from container {containerName}");

                BlobContainerClient containerClient = InternalBlobStorageClient.GetContainerClient(containerName);

                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                await blobClient.DeleteIfExistsAsync();
            }


        }

    }
}
