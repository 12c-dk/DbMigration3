using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using DbMigration.Common.Legacy.Model.Serialization;
using DbMigration.Common.Legacy.Messaging;
using DbMigration.Common.Legacy.Model;
using DbMigration.Common.Legacy.Helpers;
using DbMigration.Common.Legacy.Handlers;
using DbMigration.Common.Legacy.Model.DbConnections;
using DbMigration.Common.Legacy.Model.CopyJob;
using DbMigration.Common.Legacy.ClientStorage.Repositories;
using DbMigration.Common.Legacy.ClientStorage.Clients;

namespace DbMigration.Common.Legacy.ClientStorage.Handlers
{
    public class LoadSourceRowsMessage : IMessageData
    {
        [Required]
        public string SourceConnectionId { get; set; }
        [Required]
        public string TargetConnectionId { get; set; }
        [Required]
        public TableMapping TableMapping { get; set; }

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
            if (TableMapping == null)
            {
                throw new ArgumentNullException(nameof(TableMapping));
            }

            return true;
        }
    }

    /// <summary>
    /// Loads rows in batches (e.g.500 rows), stores data in blob and sends a Transform message for each batch. 
    /// </summary>
    public class LoadSourceRowsHandler
    {
        //DI
        private readonly DbConnectionsRepository _connectionsRepository;
        private readonly ILogger<LoadSourceRowsHandler> _log;
        private readonly MessageHelper _messageHelper;

        //Local initialized classes
        private DbConnection SourceConnection { get; set; }
        private DbConnection TargetConnection { get; set; }
        private TableMapping TableMapping { get; set; }

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions();
        private readonly QueueStorageClient _queueStorageClient;

        public LoadSourceRowsHandler(DbConnectionsRepository connectionsRepository, StorageClient internalStorageClient, ILogger<LoadSourceRowsHandler> log, MessageHelper messageHelper)
        {
            _connectionsRepository = connectionsRepository;
            _log = log;
            _messageHelper = messageHelper;

            _jsonOptions.Converters.Add(new MsEntityPropertyConverter());

            _queueStorageClient = new QueueStorageClient(internalStorageClient, Constants.Queues.Transform);
        }

        private async Task Initialize(MessageBase<LoadSourceRowsMessage> loadSourceRowsMessage)
        {
            TableMapping = loadSourceRowsMessage.Data.TableMapping;
            SourceConnection = await _connectionsRepository.GetDbConnection(loadSourceRowsMessage.Data.SourceConnectionId);
            TargetConnection = await _connectionsRepository.GetDbConnection(loadSourceRowsMessage.Data.TargetConnectionId);

        }

        /// <summary>
        /// Loads all contents of a table from the source database. Outputs rows in batches as defined by setting
        /// </summary>
        /// <param name="loadSourceRowsMessage"></param>
        /// <returns></returns>
        public async Task Handle(MessageBase<LoadSourceRowsMessage> loadSourceRowsMessage)
        {
            _log.LogInformation("LoadSourceRowsHandler.Handle called. Initializing handler.");

            try
            {
                loadSourceRowsMessage.Validate();

                await Initialize(loadSourceRowsMessage);

                var srcClient = SourceConnection.StorageClientTarget.GetTableStorageClient(TableMapping.SourceTableName);
                IList<string> selectColumns = TableMapping.ColumnMappings.Select(c => c.SourceColumnName).ToList();

                if (TableMapping.CopyUnmappedColumns)
                {
                    await srcClient.QueryWithCallbackAsync<DynamicTableEntity>(
                        ProcessQueryResults,
                        "",
                        null,
                        TableMapping.BatchSize == 0 ? null : TableMapping.BatchSize);

                }
                else
                {
                    await srcClient.QueryWithCallbackAsync<DynamicTableEntity>(ProcessQueryResults,
                        "",
                        selectColumns,
                        TableMapping.BatchSize == 0 ? null : TableMapping.BatchSize);
                }

                //Cleanup if processing went well
                await _messageHelper.CleanupMessageFile(loadSourceRowsMessage);

            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Unexpected error in LoadSourceRowsHandler");
                throw;
            }


        }

        void ProcessQueryResults<T>(List<T> queryResults, int batchNumber) where T : ITableEntity
        {
            _log.LogTrace($"LoadSourceRowsHandler ProcessQueryResults handling batch number {batchNumber}.");

            string outputRowsString;

            if (queryResults is List<DynamicTableEntity> dynamicTableEntities)
            {
                outputRowsString = TableEntitySerializer.Serialize(dynamicTableEntities);
            }
            else
            {
                outputRowsString = JsonSerializer.Serialize(queryResults, _jsonOptions);
            }

            MessageBase<TransformMessage> message = _messageHelper.BuildMessage(
                MessageType.Transform,
                outputRowsString,
                new TransformMessage()
                {
                    SourceConnectionId = SourceConnection.ConnectionId,
                    TargetConnectionId = TargetConnection.ConnectionId
                }
            ).Result;

            //Send message to storage queue
            var transformRowsJson = JsonSerializer.Serialize(message);
            _queueStorageClient.SendMessage(transformRowsJson).Wait();

        }


    }
}
