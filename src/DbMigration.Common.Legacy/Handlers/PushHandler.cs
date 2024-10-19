using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using DbMigration.Common.Legacy.ClientStorage.Clients;
using DbMigration.Common.Legacy.ClientStorage.Repositories;
using DbMigration.Common.Legacy.Helpers;
using DbMigration.Common.Legacy.Messaging;
using DbMigration.Common.Legacy.Model.DbConnections;
using DbMigration.Common.Legacy.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace DbMigration.Common.Legacy.Handlers
{
    public class PushMessage : IMessageData
    {
        [Required]
        public string TargetConnectionId { get; set; }


        public bool IsValid()
        {
            if (TargetConnectionId == null)
            {
                throw new ArgumentNullException(nameof(TargetConnectionId));
            }
            return true;
        }
    }

    /// <summary>
    /// Receives messages with reference to blobs containing data rows. Pushes data to target database.
    /// </summary>
    public class PushHandler
    {
        private readonly DbConnectionsRepository _connectionsRepository;
        private readonly StorageClient _internalStorageClient;
        private readonly ILogger<PushHandler> _log;
        private readonly MessageHelper _messageHelper;

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions();
        private DbConnection TargetConnection { get; set; }

        public PushHandler(DbConnectionsRepository connectionsRepository, StorageClient internalStorageClient, ILogger<PushHandler> log, MessageHelper messageHelper)
        {
            _connectionsRepository = connectionsRepository;
            _internalStorageClient = internalStorageClient;
            _log = log;
            _messageHelper = messageHelper;
            _jsonOptions.Converters.Add(new MsEntityPropertyConverter());

        }

        private async Task Initialize(MessageBase<PushMessage> pushMessage)
        {
            pushMessage.Validate(true);

            TargetConnection = await _connectionsRepository.GetDbConnection(pushMessage.Data.TargetConnectionId);

            ValidateHandler();
        }

        private void ValidateHandler()
        {
            if (TargetConnection == null)
            {
                throw new ArgumentNullException(nameof(TargetConnection));
            }
            if (_internalStorageClient == null)
            {
                throw new ArgumentNullException(nameof(_internalStorageClient));
            }
            TargetConnection.Validate();
        }


        public async Task Handle(MessageBase<PushMessage> pushMessage)
        {
            try
            {
                await Initialize(pushMessage);

                //var messageFileContent = await _messageHelper.GetMessageFileContent(pushMessage);

                //List<TableEntity> rows = JsonSerializer.Deserialize<List<TableEntity>>(messageFileContent, _jsonOptions);

                //Cleanup if processing went well
                await _messageHelper.CleanupMessageFile(pushMessage);

            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Unexpected error in PushHandler");
                throw;
            }


        }
    }
}
