using DbMigration.Domain.DictionaryModels;
using DbMigration.Domain.Model;

namespace DbMigration.Sync.Interfaces
{
    public interface IAdapter
    {
        // Implement:
        // GetSchema
        // GetItem (by id)
        // Method to fetch data from the source
        
        Task<DbOperationResponse> SetConfiguration<T>(T configuration);

        Task<List<DbItem>> GetTableData(string tableName, int? top = null, List<string> selectFields = null,
            string queryString = null);
        public Task<DbValueCollectionOperationResponse<List<DbItem>>> UpdateRows(string tableName, List<DbItem> items);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="data"></param>
        /// <returns>Tuple of successItems as dictionary of input items and output items and DbOperationResponse with failed items</returns>
        public Task<DbValueCollectionOperationResponse<Dictionary<DbItem, DbItem>>> InsertRows(string tableName,
            List<DbItem> data);

        Task<DbValueCollectionOperationResponse<List<DbItem>>> DeleteItems(string tableName, List<DbItem> items);

        Task<DbValueCollectionOperationResponse<List<DbItem>>> UpsertRows(string tableName, List<DbItem> items);

        Task<bool> TestConnection();
    }
}
