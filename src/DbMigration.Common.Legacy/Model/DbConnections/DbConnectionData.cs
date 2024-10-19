using Microsoft.WindowsAzure.Storage.Table;

namespace DbMigration.Common.Legacy.Model.DbConnections
{
    public class DbConnectionData : TableEntity
    {
        public string Name { get; set; }
        public string ConnectionString { get; set; }

        public bool Verified { get; set; } = true;

        //Todo: Add support for storing tables, and indexes for tables. 

        public CloudTableClient GetTableClient()
        {
            //todo: implement. And store for future use. This should be moved to new DbConnection class, that has the functionality. 

            throw new NotImplementedException();
        }


    }
}
