using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Azure.Storage.Blobs;
using DbMigration.Common.Legacy.ClientStorage.Clients;
using DbMigration.Common.Legacy.ClientStorage.Repositories;
using DbMigration.Common.Legacy.Helpers;
using DbMigration.Common.Legacy.Messaging;
using DbMigration.Common.Legacy.Model;
using DbMigration.Common.Legacy.Model.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;

namespace DbMigration.Common.Legacy.Handlers
{
    public class TransformMessage : IMessageData
    {
        [Required]
        public string SourceConnectionId { get; set; }
        [Required]
        public string TargetConnectionId { get; set; }


        public bool IsValid()
        {
            if (SourceConnectionId == null)
            {
                throw new ArgumentNullException(nameof(SourceConnectionId));
            }
            if (TargetConnectionId == null)
            {
                throw new ArgumentNullException(nameof(TargetConnectionId));
            }
            return true;
        }
    }

    /// <summary>
    /// Loads blob with row data and executes transformation tasks.
    /// </summary>
    public class TransformHandler
    {
        //DI
        // ReSharper disable once NotAccessedField.Local
        private readonly DbConnectionsRepository _connectionsRepository;
        private readonly StorageClient _internalStorageClient;
        private readonly ILogger<TransformHandler> _log;
        private BlobContainerClient _containerClient;
        private readonly MessageHelper _messageHelper;

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions();
        private readonly QueueStorageClient _queueStorageClient;

        public TransformHandler(DbConnectionsRepository connectionsRepository, StorageClient internalStorageClient, ILogger<TransformHandler> log, MessageHelper messageHelper)
        {
            _connectionsRepository = connectionsRepository;
            _internalStorageClient = internalStorageClient;
            _log = log;
            _messageHelper = messageHelper;
            _jsonOptions.Converters.Add(new MsEntityPropertyConverter());

            _queueStorageClient = new QueueStorageClient(internalStorageClient, Constants.Queues.Transform);
        }

        public async Task Handle(MessageBase<TransformMessage> transformMessage)
        {
            try
            {
                transformMessage.Validate(true);

                BlobUriBuilder blobUriBuilder = new BlobUriBuilder(new Uri(transformMessage.MessageFilePath));
                string containerName = blobUriBuilder.BlobContainerName;

                BlobStorageClient blobStorageClient = _internalStorageClient.GetBlobStorageClient();
                _containerClient = blobStorageClient.GetContainerClient(containerName);

                BlobClient blobClient = _containerClient.GetBlobClient(blobUriBuilder.BlobName);

                var content = await blobClient.DownloadContentAsync();

                string rowsJson = content.Value.Content.ToString();

                List<DynamicTableEntity> rows = TableEntitySerializer.Deserialize(rowsJson);


                //todo: Implement transformation based on configuration. 

                var serializedOutput = TableEntitySerializer.Serialize(rows);

                //Todo: Setup class to create message of a type, pushing content to file. And setup same class to get content from file. Should generate guid for filename. 

                var message = await _messageHelper.BuildMessage(
                    MessageType.PushRows,
                    serializedOutput,
                    new TransformMessage()
                    {
                        SourceConnectionId = transformMessage.Data.SourceConnectionId,
                        TargetConnectionId = transformMessage.Data.TargetConnectionId
                    }
                );

                //Send message to storage queue
                var pushRowsJson = JsonSerializer.Serialize(message);
                _queueStorageClient.SendMessage(pushRowsJson).Wait();

                await _messageHelper.CleanupMessageFile(transformMessage);

            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Unexpected error in TransformHandler");
                throw;
            }



        }

    }
}
