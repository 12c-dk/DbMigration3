using DbMigration.Common.Legacy.ClientStorage.Repositories;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace DbMigration.Common.Legacy.ClientStorage.Clients
{
    public class StorageClient
    {
        //todo: Implement use of the new Azure.Data.Tables (https://medium.com/medialesson/which-azure-table-storage-net-sdk-should-i-use-a7f33fe294e)

        internal readonly CloudStorageAccount CloudStorageAccount;
        private readonly CloudTableClient _tableClient;
        private readonly ConfigManager _configManager;
        internal readonly string StorageConnectionString;

        public StorageClient(string storageConnectionString, ConfigManager configManager)
        {
            StorageConnectionString = storageConnectionString;
            _configManager = configManager;
            CloudStorageAccount = CloudStorageAccount.Parse(storageConnectionString);

            _tableClient = CloudStorageAccount.CreateCloudTableClient();

        }

        public TableStorageClient GetTableStorageClient(string tableName)
        {
            TableStorageClient client = new TableStorageClient(this, tableName, _configManager);
            return client;
        }

        public QueueStorageClient GetQueueStorageClient(string queueName)
        {
            QueueStorageClient client = new QueueStorageClient(this, queueName);
            return client;
        }

        public BlobStorageClient GetBlobStorageClient()
        {
            BlobStorageClient client = new BlobStorageClient(this);
            return client;
        }

        public async Task<List<CloudTable>> GetTables()
        {
            TableContinuationToken continuationToken = null;
            var allTables = new List<CloudTable>();
            do
            {
                var listingResult = await _tableClient.ListTablesSegmentedAsync(continuationToken);
                var tables = listingResult.Results.ToList();
                //listingResult.Results.ToList
                continuationToken = listingResult.ContinuationToken;
                //Add the tables to your allTables
                allTables.AddRange(tables);
            }
            while (continuationToken != null);

            return allTables;
        }

        public async Task CreateTable(string tableName)
        {
            var table = _tableClient.GetTableReference(tableName);
            //Table name can only contain letters and numbers

            await table.CreateIfNotExistsAsync();
        }

    }
}
