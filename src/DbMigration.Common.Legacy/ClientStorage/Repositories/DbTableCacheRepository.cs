using DbMigration.Common.Legacy.ClientStorage.Clients;
using DbMigration.Common.Legacy.Model.DbConnections;
using Microsoft.WindowsAzure.Storage.Table;

namespace DbMigration.Common.Legacy.ClientStorage.Repositories
{

    /// <summary>
    /// This class is initialized using DI with the internal storage client. 
    /// </summary>
    public class DbTableCacheRepository
    {
        private readonly StorageClient _internalStorageClient;

        /// <summary>
        /// TableClient for dbConnections (DbConnectionData) with the table name: Tbl&lt;ConId&gt;
        /// </summary>
        private TableStorageClient _tableCacheClient;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="internalStorageClient"></param>
        public DbTableCacheRepository(StorageClient internalStorageClient)
        {
            _internalStorageClient = internalStorageClient;
        }

        private void InitializeTable(string connectionId)
        {
            string dbTableTableName = $"Tbl{connectionId?.Replace("-", "")}";
            _tableCacheClient = _internalStorageClient.GetTableStorageClient(dbTableTableName);
            _tableCacheClient.Table.CreateIfNotExistsAsync();
        }

        /// <summary>
        /// Updates the Tables cache for a connection (Tbl&lt;ConId&gt;). Called from DbConnection.LoadTables. 
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="tables"></param>
        /// <returns></returns>
        public async Task UpdateTableCache(string connectionId, List<CloudTable> tables)
        {
            InitializeTable(connectionId);

            List<DbTableData> existingTableCacheRows = await _tableCacheClient.QueryAsync<DbTableData>();

            foreach (CloudTable table in tables)
            {
                DbTableData tableRecord;

                if (existingTableCacheRows.FirstOrDefault(t => t.TableName == table.Name) != null)
                {
                    tableRecord = existingTableCacheRows.First(t => t.TableName == table.Name);
                    tableRecord.UpdatedTime = DateTime.Now;
                }
                else
                {
                    tableRecord = new DbTableData()
                    {
                        PartitionKey = "Table",
                        RowKey = Guid.NewGuid().ToString(),
                        TableName = table.Name,
                        UpdatedTime = DateTime.Now
                    };
                }

                await _tableCacheClient.InsertOrMerge(tableRecord);

            }
        }
    }
}
