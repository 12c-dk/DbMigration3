using Microsoft.Extensions.Logging;
using DbMigration.Common.Legacy.Messaging;
using DbMigration.Common.Legacy.Model;
using DbMigration.Common.Legacy.Helpers;
using DbMigration.Common.Legacy.Model.DbConnections;
using DbMigration.Common.Legacy.Model.CopyJob;
using DbMigration.Common.Legacy.ClientStorage.Repositories;
using DbMigration.Common.Legacy.ClientStorage.Handlers;
using DbMigration.Common.Legacy.ClientStorage.Clients;

namespace DbMigration.Common.Legacy.Handlers
{
    public class CopyJobMessage : IMessageData
    {
        public string SourceConnectionId { get; set; }
        public string TargetConnectionId { get; set; }
        public MappingConfiguration Mapping { get; set; }

        //todo: Implement following settings?
        //  Rewrite deleted rows
        //      Source and destination tables

        //  UpdateCheck by Etag
        //  UpdateCheck by Data (Columns to check)
        //  UpdateCheck by Date field (Specific column to check)


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
            if (Mapping == null)
            {
                throw new ArgumentNullException(nameof(Mapping));
            }

            return true;
        }
    }

    /// <summary>
    /// Receives mapping configuration, iterates tables and sends LoadSourceRowsMessage for each table.
    /// </summary>
    public class CopyJobHandler
    {
        //DI
        private readonly DbConnectionsRepository _connectionsRepository;
        private readonly StorageClient _internalStorageClient;
        private readonly ILogger<CopyJobHandler> _log;
        private readonly MessageHelper _messageHelper;

        //Local initialized classes
        private DbConnection SourceConnection { get; set; }
        private DbConnection TargetConnection { get; set; }
        private MappingConfiguration Mapping { get; set; }

        public CopyJobHandler(DbConnectionsRepository connectionsRepository, StorageClient internalStorageClient, ILogger<CopyJobHandler> log, MessageHelper messageHelper)
        {
            _connectionsRepository = connectionsRepository;
            _internalStorageClient = internalStorageClient;
            _log = log;
            _messageHelper = messageHelper;
        }

        private void ValidateHandler()
        {
            if (SourceConnection == null)
            {
                throw new ArgumentNullException(nameof(SourceConnection));
            }
            if (TargetConnection == null)
            {
                throw new ArgumentNullException(nameof(TargetConnection));
            }
            if (_internalStorageClient == null)
            {
                throw new ArgumentNullException(nameof(_internalStorageClient));
            }
            if (Mapping == null)
            {
                throw new ArgumentNullException(nameof(Mapping));
            }
            SourceConnection.Validate();
            TargetConnection.Validate();
        }

        private async Task Initialize(MessageBase<CopyJobMessage> copyMessage)
        {
            copyMessage.Validate();

            Mapping = copyMessage.Data.Mapping;
            SourceConnection = await _connectionsRepository.GetDbConnection(copyMessage.Data.SourceConnectionId);
            TargetConnection = await _connectionsRepository.GetDbConnection(copyMessage.Data.TargetConnectionId);

            ValidateHandler();
        }

        public async Task Handle(MessageBase<CopyJobMessage> copyMessage)
        {
            _log.LogInformation("CopyJobHandler.Handle called. Initializing handler.");

            try
            {
                await Initialize(copyMessage);

                List<TableMapping> tableMappings = new List<TableMapping>();

                if (Mapping.CopyUnmappedTables)
                {
                    var tables = await SourceConnection.StorageClientTarget.GetTables();
                    foreach (var table in tables)
                    {

                        //.All corresponds to !..Any
                        if (Mapping.TableMappings.All(x => x.SourceTableName != table.Name))
                        {
                            tableMappings.Add(new TableMapping()
                            {
                                SourceTableName = table.Name,
                                TargetTableName = table.Name
                            });
                        }
                    }
                }

                tableMappings.AddRange(Mapping.TableMappings);

                foreach (TableMapping tableMapping in tableMappings)
                {
                    _log.LogInformation("Creating LoadSourceRows message for source table {0}", tableMapping.SourceTableName);

                    MessageBase<LoadSourceRowsMessage> loadSourceRowsMessage2 = await _messageHelper.BuildMessage(MessageType.LoadSourceRows, null, new LoadSourceRowsMessage()
                    {
                        SourceConnectionId = SourceConnection.ConnectionId,
                        TargetConnectionId = TargetConnection.ConnectionId,
                        TableMapping = tableMapping
                    });

                    //Send message to storage queue
                    var loadSourceRowsJson = System.Text.Json.JsonSerializer.Serialize(loadSourceRowsMessage2);
                    var queueStorageClient = _internalStorageClient.GetQueueStorageClient(Constants.Queues.Transform);
                    await queueStorageClient.SendMessage(loadSourceRowsJson);
                }

            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Unexpected error in CopyJobHandler");
                throw;
            }


            //todo: implement table mapping
            //todo: Implement update configuration.
            //  All rows / 
            //  Copy unmapped columns y/n
            //  Mapped columns(collection)
            //  Rewrite deleted rows
            //      Source and destination tables
            //  UpdateCheck by Etag
            //  UpdateCheck by Data (Columns to check)
            //  UpdateCheck by Date field (Specific column to check)


            //Get job tables (Mapping)
            //For each table Setup job for copying rows from source to cache


            //TableMapping
            //SourceConnection.

        }

    }
}
