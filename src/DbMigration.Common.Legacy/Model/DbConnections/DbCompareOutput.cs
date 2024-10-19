using DbMigration.Common.Legacy.Model.Jobs;

namespace DbMigration.Common.Legacy.Model.DbConnections
{
    public class DbCompareOutput
    {
        public List<DbIndexData> NewRows { get; set; } = new List<DbIndexData>();
        public List<DbIndexData> UpdatedRows { get; set; } = new List<DbIndexData>();
        public List<DbIndexData> DeletedRows { get; set; } = new List<DbIndexData>();
        public List<DbIndexData> SkippedRows { get; set; } = new List<DbIndexData>();
        public List<DbIndexData> FailedRows { get; set; } = new List<DbIndexData>();

        public DbJobStatistics Statistics { get; set; } = new DbJobStatistics();
    }
}
