using DbMigration.Common.Legacy.ClientStorage.Clients;
using DbMigration.Common.Legacy.Model.DbConnections;

namespace DbMigration.Common.Legacy.ClientStorage.Repositories
{
    public class DbIndexCacheRepository
    {
        private readonly StorageClient _storageClient;

        /// <summary>
        /// TableClient for dbConnections (DbConnectionData) with the table name: Idx&lt;ConId&gt;&lt;TblId&gt;
        /// </summary>
        public TableStorageClient DbTableClient;

        public DbIndexCacheRepository(StorageClient storageClient)
        {
            _storageClient = storageClient;
        }

        /// <summary>
        /// Initializes the index table (Idx&lt;TableId&gt;)
        /// </summary>
        /// <param name="connectionId">The DbConnection Id</param>
        /// <param name="tableId">The RowKey from the table entry in the internal table cache (Tbl&lt;ConId&gt;) used for the target Internal Index Idx&lt;ConId&gt;</param>
        /// <exception cref="ArgumentNullException"></exception>
        public void InitializeTable(string connectionId, string tableId)
        {
            if (string.IsNullOrEmpty(connectionId)) throw new ArgumentNullException(connectionId);
            if (string.IsNullOrEmpty(tableId)) throw new ArgumentNullException(tableId);

            string dbTableTableName = $"Idx{tableId.Replace("-", "")}";
            DbTableClient = _storageClient.GetTableStorageClient(dbTableTableName);
            DbTableClient.Table.CreateIfNotExistsAsync();

        }

        //public int GetIndexCount(string tableName)
        //{

        //}

        public async Task PushIndexes(string connectionId, string tableId, DbCompareOutput compareOutput)
        {
            InitializeTable(connectionId, tableId);

            await DbTableClient.InsertOrMergeBatch(compareOutput.NewRows);

            await DbTableClient.InsertOrMergeBatch(compareOutput.UpdatedRows);

            //Updates the internal Index cache with the status 'Deleted'
            await DbTableClient.InsertOrMergeBatch(compareOutput.DeletedRows);


        }
    }
}
