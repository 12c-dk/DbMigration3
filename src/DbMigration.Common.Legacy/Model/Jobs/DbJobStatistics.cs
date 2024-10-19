namespace DbMigration.Common.Legacy.Model.Jobs
{
    public class DbJobStatistics
    {
        public int RowsNew { get; set; }
        public int RowsUpdated { get; set; }
        public int RowsDeleted { get; set; }
        public int RowsSkipped { get; set; }
        public int RowsFailed { get; set; }

        /// <summary>
        /// Dictionary contains Key = unique identifier of row and Value = error message. 
        /// </summary>
        public Dictionary<string, string> Errors { get; set; }

        public void Append(DbJobStatistics stats)
        {
            RowsNew += stats.RowsNew;
            RowsUpdated += stats.RowsUpdated;
            RowsDeleted += stats.RowsDeleted;
            RowsSkipped += stats.RowsSkipped;
            RowsFailed += stats.RowsFailed;
        }

        public override string ToString()
        {
            return $"New rows: {RowsNew} Updated: {RowsUpdated} Deleted: {RowsDeleted} Skipped: {RowsSkipped} Failed: {RowsFailed}";
        }
    }
}
