using Microsoft.WindowsAzure.Storage.Table;

namespace DbMigration.Common.Legacy.Model
{
    public class Project : TableEntity
    {
        public string Name { get; set; }
        public string Title { get; set; }

    }
}
