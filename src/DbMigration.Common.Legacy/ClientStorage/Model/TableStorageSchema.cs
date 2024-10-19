namespace DbMigration.Common.Legacy.ClientStorage.Model
{
    public class TableStorageSchema
    {
        public string TableName { get; set; }
        public readonly List<TableStorageField> Fields = new List<TableStorageField>();

    }
}
