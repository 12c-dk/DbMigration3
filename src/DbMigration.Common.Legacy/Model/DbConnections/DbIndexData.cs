using Microsoft.WindowsAzure.Storage.Table;

namespace DbMigration.Common.Legacy.Model.DbConnections
{
    public class DbIndexData : TableEntity
    {
        //Status may be Created (Indexed), Updated (SrcEtag doesn't match) or Deleted (Not found in Source)
        public string Status { get; set; }
        public string ConnectionId { get; set; }
        public string SrcEtag { get; set; }

    }
}
