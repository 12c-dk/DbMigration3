namespace DbMigration.Common.Legacy.Model.CopyJob
{
    /// <summary>
    /// Represents a mapping between a source table and a target table for a database migration job.
    /// </summary>
    public class TableMapping
    {
        public string SourceTableName { get; set; }
        public string TargetTableName { get; set; }

        public int RowLimit { get; set; } = 0;
        public int BatchSize { get; set; } = 200;

        public bool CopyUnmappedColumns { get; set; } = false;

        public List<ColumnMapping> ColumnMappings { get; set; } = new List<ColumnMapping>();
    }
}
