using DbMigration.Common.Legacy.ClientStorage.Clients;
using DbMigration.Common.Legacy.ClientStorage.Repositories;
using DbMigration.Common.Legacy.Model.Jobs;
using Microsoft.WindowsAzure.Storage.Table;

namespace DbMigration.Common.Legacy.Model.DbConnections
{
    public class DbConnection
    {
        //todo: DbConnection should be provided by DbConnectionRepository

        public readonly DbConnectionData DbConnectionData;

        /// <summary>
        /// Refers to the rowKey in the DbConnections table. 
        /// </summary>
        public readonly string ConnectionId;

        /// <summary>
        /// Connection to Internal storage for access to dbconnections table, And tables table (Tbl&lt;ConId&gt;)
        /// </summary>
        private readonly StorageClient _storageClientInternal;
        private readonly TableStorageClient _connectionTableClient;

        private readonly DbTableCacheRepository _dbTableRepository;
        private readonly DbIndexCacheRepository _dbIndexRepository;

        public readonly StorageClient StorageClientTarget;

        public DbConnection(StorageClient storageClientInternal, DbConnectionData dbConnectionData, DbTableCacheRepository dbTableRepository, DbIndexCacheRepository dbIndexRepository, ConfigManager configManager)
        {
            DbConnectionData = dbConnectionData;
            _dbTableRepository = dbTableRepository;
            _dbIndexRepository = dbIndexRepository;
            _storageClientInternal = storageClientInternal;
            ConnectionId = dbConnectionData?.RowKey;

            StorageClientTarget = new StorageClient(DbConnectionData?.ConnectionString, configManager);

            Validate();

            string connectionTableTableName = $"Tbl{ConnectionId?.Replace("-", "")}";
            _connectionTableClient = _storageClientInternal.GetTableStorageClient(connectionTableTableName);
            _storageClientInternal.CreateTable(connectionTableTableName).Wait();

        }

        public DbConnection(StorageClient storageClientInternal, string connectionId, DbTableCacheRepository dbTableRepository, DbIndexCacheRepository dbIndexRepository, ConfigManager configManager)
        {
            _storageClientInternal = storageClientInternal;
            ConnectionId = connectionId;
            _dbTableRepository = dbTableRepository;
            _dbIndexRepository = dbIndexRepository;

            var tableClient = _storageClientInternal.GetTableStorageClient("dbconnections");
            DbConnectionData dbConnectionData = tableClient.Retrieve<DbConnectionData>("dbconnection", connectionId).Result;
            DbConnectionData = dbConnectionData;


            string connectionTableTableName = $"Tbl{ConnectionId.Replace("-", "")}";
            _storageClientInternal.CreateTable(connectionTableTableName).Wait();
            _connectionTableClient = _storageClientInternal.GetTableStorageClient(connectionTableTableName);
            //Todo: Maybe _connectionTableClient should be handled in DbTableRepository _dbTableRepository

            StorageClientTarget = new StorageClient(DbConnectionData.ConnectionString, configManager);

            Validate();
        }


        public string ConnectionTableTableName => $"Tbl{ConnectionId.Replace("-", "")}";

        public string IndexTableTableName(string targetTableName, string internalTableId)
        {
            //Idx<ConId><TblId>
            return $"Tbl{ConnectionId.Replace("-", "")}{internalTableId.Replace("-", "")}";

        }

        public void Validate()
        {
            if (string.IsNullOrEmpty(DbConnectionData?.Name))
            {
                throw new ArgumentNullException(nameof(DbConnectionData.Name));
            }

            if (string.IsNullOrEmpty(ConnectionId))
            {
                throw new ArgumentNullException(nameof(ConnectionId));
            }

            if (_storageClientInternal == null)
            {
                throw new ArgumentNullException(nameof(StorageClient));
            }


        }

        //Gets tables from target account and stores them using the DbTableRepository in the Tbl<ConId>
        public async Task LoadTables()
        {
            List<CloudTable> tables = await StorageClientTarget.GetTables();

            await _dbTableRepository.UpdateTableCache(ConnectionId, tables);
        }


        /// <summary>
        /// Updates local cache of the Index in Idx&lt;TblId&gt;. Calculate Created, updated and deleted rows and push the changes using InsertOrMergeBatch and write statistics
        /// </summary>
        /// <param name="maxLength"></param>
        /// <returns>DbJobStatistics containing a summary of affected rows</returns>
        public async Task<DbJobStatistics> LoadIndexes(int maxLength)
        {
            var tables = _connectionTableClient.Query<DbTableData>();
            DbJobStatistics statistics = new DbJobStatistics();
            foreach (var table in tables)
            {
                var srcTableClient = StorageClientTarget.GetTableStorageClient(table.TableName);

                DbCompare compare = new DbCompare();

                var srcIndexRows = await srcTableClient.QueryAsync<DbIndexData>(selectColumns: new List<string>() { "PartitionKey", "RowKey" });

                //Initialize the index table (Idx<TblId>)
                _dbIndexRepository.InitializeTable(ConnectionId, table.RowKey);
                var existingCachedIndexes =
                    await _dbIndexRepository.DbTableClient.QueryAsync<DbIndexData>(selectColumns: new List<string>()
                        {"PartitionKey", "RowKey", "SrcEtag", "Status"});

                DbCompareOutput compareResult = compare.CompareSrcToIndex(srcIndexRows, existingCachedIndexes);

                await _dbIndexRepository.PushIndexes(ConnectionId, table.RowKey, compareResult);

                statistics.Append(compareResult.Statistics);

                //Todo: Use QueryWithCallback

            }

            return statistics;

        }
    }
}
