using Microsoft.WindowsAzure.Storage.Table;

namespace DbMigration.Common.Legacy.Model.DbConnections
{
    internal class DbTableData : TableEntity
    {
        public int? RowCount { get; set; }
        public string TableName { get; set; }
        public DateTime UpdatedTime { get; set; }
        public DateTime IndexedTime { get; set; }


    }
}
