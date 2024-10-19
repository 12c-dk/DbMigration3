using Microsoft.WindowsAzure.Storage.Table;

namespace DbMigration.Common.Legacy.ClientStorage.Model
{
    public class TableStorageField
    {
        //IsPrimaryKey is true for keys that uniquely identifies a row. For table storage this is only PartitionKey and RowKey
        public bool IsIdentifier { get; set; }
        public string FieldName { get; set; }
        public EdmType FieldType { get; set; }

    }
}
