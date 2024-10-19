using DbMigration.Common.Legacy.ClientStorage.Clients;
using DbMigration.Common.Legacy.Model;
using DbMigration.Common.Legacy.Model.DbConnections;

namespace DbMigration.Common.Legacy.ClientStorage.Repositories
{
    public class DbConnectionsRepository
    {
        /// <summary>
        /// TableClient for dbConnections (DbConnectionData)
        /// </summary>
        private readonly TableStorageClient _tableStorageClient;

        /// <summary>
        /// StorageClient connected to the internal storage account
        /// </summary>
        private readonly StorageClient _storageClient;

        private readonly DbTableCacheRepository _dbTableCacheRepository;
        private readonly DbIndexCacheRepository _dbIndexCacheRepository;
        private readonly ConfigManager _configManager;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="storageClient">A StorageClient connected to the internal storage account</param>
        /// <param name="dbTableCacheRepository"></param>
        /// <param name="dbIndexCacheRepository"></param>
        /// <param name="configManager"></param>
        public DbConnectionsRepository(StorageClient storageClient, DbTableCacheRepository dbTableCacheRepository, DbIndexCacheRepository dbIndexCacheRepository, ConfigManager configManager)
        {
            _storageClient = storageClient;
            _dbTableCacheRepository = dbTableCacheRepository;
            _dbIndexCacheRepository = dbIndexCacheRepository;
            _configManager = configManager;
            _tableStorageClient = storageClient.GetTableStorageClient("dbconnections");
            _tableStorageClient.Table.CreateIfNotExistsAsync();

        }

        public async Task<List<DbConnectionData>> GetDbConnectionDatas()
        {
            var connections = await _tableStorageClient.QueryAsync<DbConnectionData>(takeCount: 10);
            return connections;
        }

        public async Task<DbConnectionData> GetDbConnectionData(string id)
        {
            var connection = await _tableStorageClient.Retrieve<DbConnectionData>(Constants.Tables.DbConnections.PartitionKey, id);
            return connection;
        }

        public async Task<DbConnectionData> GetDbConnectionDataByName(string name)
        {
            var connections = await _tableStorageClient.QueryAsync<DbConnectionData>();
            DbConnectionData selectedConnection = connections.Single(c => c.Name == name);
            return selectedConnection;

        }

        public async Task<DbConnection> GetDbConnection(string id)
        {
            DbConnectionData connectionData = await _tableStorageClient.Retrieve<DbConnectionData>(Constants.Tables.DbConnections.PartitionKey, id);
            DbConnection con = new DbConnection(_storageClient, connectionData, _dbTableCacheRepository, _dbIndexCacheRepository, _configManager);

            return con;
        }

        public async Task<DbConnection> GetDbConnectionByName(string name)
        {
            var connections = await _tableStorageClient.QueryAsync();
            var selectedConnection = connections.Single(c => c.Properties["Name"].StringValue == name);

            DbConnectionData connectionData = await _tableStorageClient.Retrieve<DbConnectionData>(Constants.Tables.DbConnections.PartitionKey, selectedConnection.RowKey);
            DbConnection con = new DbConnection(_storageClient, connectionData, _dbTableCacheRepository, _dbIndexCacheRepository, _configManager);

            return con;
        }


        public async Task SaveDbConnection(DbConnectionData connection)
        {
            await _tableStorageClient.InsertOrMerge(connection);
        }


    }
}
