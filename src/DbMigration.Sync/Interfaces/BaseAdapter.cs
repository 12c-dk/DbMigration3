using DbMigration.Domain.DictionaryModels;
using DbMigration.Domain.Model;

namespace DbMigration.Sync.Interfaces
{
    public abstract class BaseAdapter : IAdapter
    {
        public abstract Task<DbOperationResponse> SetConfiguration<T>(T configuration);
        public abstract Task<List<DbItem>> GetTableData(string tableName, int? top = null, List<string> selectFields = null,
            string queryString = null);

        public abstract Task<DbValueCollectionOperationResponse<List<DbItem>>> UpdateRows(string tableName, List<DbItem> items);

        public abstract Task<DbValueCollectionOperationResponse<Dictionary<DbItem, DbItem>>> InsertRows(string tableName,
            List<DbItem> data);

        public abstract Task<DbValueCollectionOperationResponse<List<DbItem>>> DeleteItems(string tableName, List<DbItem> items);
        
        public abstract Task<bool> TestConnection();

        public virtual async Task<DbValueCollectionOperationResponse<List<DbItem>>> UpsertRows(string tableName, List<DbItem> items)
        {
            // Fetch existing data
            List<DbItem> existingData = await GetTableData(tableName);
            //Split the target table data into Identifiers and Data

            existingData = existingData.DataToDbItemsWithIdentifiers(new string[] { "Id" });

            // Separate rows into those that need to be updated and those that need to be inserted
            var itemsToUpdate = new List<DbItem>();
            var itemsToInsert = new List<DbItem>();

            foreach (var item in items)
            {
                //Problem: ExistingData has only Data. Item has split Identifiers and Data, so Identifiers dont match
                var existingItem = existingData.FirstOrDefault(e => e.Identifiers.SequenceEqual(item.Identifiers));
                if (existingItem != null)
                {
                    itemsToUpdate.Add(item);
                }
                else
                {
                    itemsToInsert.Add(item);
                }
            }

            // Update existing rows
            DbValueCollectionOperationResponse<List<DbItem>> upsertResponse = await UpdateRows(tableName, itemsToUpdate);

            // Insert new rows
            var insertResponse = await InsertRows(tableName, itemsToInsert);

            
            upsertResponse.OperationResponse.Append(insertResponse.OperationResponse);
            upsertResponse.ResponseValue =
                upsertResponse.ResponseValue.Concat(insertResponse.ResponseValue.Values).ToList();


            return upsertResponse;
        }
    }
}
