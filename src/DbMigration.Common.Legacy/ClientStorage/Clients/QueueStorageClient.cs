using System.Text;
using Azure;
using Azure.Storage.Queues; // Namespace for Queue storage types
using Azure.Storage.Queues.Models;
using Microsoft.WindowsAzure.Storage;
// Namespace for PeekedMessage

namespace DbMigration.Common.Legacy.ClientStorage.Clients
{
    public class QueueStorageClient
    {
        private readonly QueueClient _queueClient;

        public QueueStorageClient(StorageClient storageClient, string queueName)
        {
            _queueClient = new QueueClient(storageClient.StorageConnectionString, queueName);
            try
            {
                _queueClient.CreateIfNotExists();
            }
            catch (AggregateException e)
            {
                var innerException = e.InnerException;

                if (innerException != null && innerException.GetType() == typeof(StorageException))
                {
                    var storageException = (StorageException)innerException;
                    throw new ApplicationException("Error during queue creation. " + storageException.RequestInformation?.ExtendedErrorInformation?.ErrorMessage, e);
                }

                throw new ApplicationException("Error during queue creation. Unknown exception type. " + e.Message, e);

            }

        }

        public async Task<Response<SendReceipt>> SendMessage(string messageJson, TimeSpan? visibilityTimeout = null, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(messageJson);
            string base64Message = Convert.ToBase64String(messageBytes);
            Response<SendReceipt> result = await _queueClient.SendMessageAsync(base64Message, visibilityTimeout, timeToLive, cancellationToken);
            return result;
        }


    }
}
