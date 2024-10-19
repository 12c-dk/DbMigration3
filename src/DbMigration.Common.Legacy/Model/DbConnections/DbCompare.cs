namespace DbMigration.Common.Legacy.Model.DbConnections
{
    public class DbCompare
    {
        /// <summary>
        /// Compares two lists of DbIndexData objects representing indexes of a database and generates a comparison output.
        /// </summary>
        /// <param name="srcIndexes">The list of DbIndexData objects to compare.</param>
        /// <param name="targetExistingIndexes">The list of existing DbIndexData objects to compare against.</param>
        /// <returns>A DbCompareOutput object containing information about rows that were added, updated, deleted, or skipped based on if their matching index was found in the other list.</returns>
        public DbCompareOutput CompareSrcToIndex(List<DbIndexData> srcIndexes, List<DbIndexData> targetExistingIndexes)
        {
            if (srcIndexes == null) throw new ArgumentNullException(nameof(srcIndexes));
            if (targetExistingIndexes == null) throw new ArgumentNullException(nameof(targetExistingIndexes));

            DbCompareOutput output = new DbCompareOutput();

            foreach (var srcIndex in srcIndexes)
            {
                srcIndex.SrcEtag = srcIndex.ETag;

                var targetIndex = targetExistingIndexes.FirstOrDefault(x =>
                    x.RowKey == srcIndex.RowKey && x.PartitionKey == srcIndex.PartitionKey);
                if (targetIndex == null)
                {
                    srcIndex.Status = "Created";
                    output.NewRows.Add(srcIndex);
                    output.Statistics.RowsNew++;
                }
                else
                {
                    if (targetIndex.SrcEtag != srcIndex.ETag)
                    {
                        srcIndex.Status = "Updated";
                        output.UpdatedRows.Add(srcIndex);
                        output.Statistics.RowsUpdated++;
                    }
                    else
                    {
                        srcIndex.Status = "Skipped";
                        output.SkippedRows.Add(srcIndex);
                        output.Statistics.RowsSkipped++;
                    }
                }
            }

            foreach (var targetIndex in targetExistingIndexes)
            {
                var srcIndex = srcIndexes.FirstOrDefault(x =>
                    x.RowKey == targetIndex.RowKey && x.PartitionKey == targetIndex.PartitionKey);
                if (srcIndex == null && targetIndex.Status != "Deleted")
                {
                    targetIndex.Status = "Deleted";
                    output.DeletedRows.Add(targetIndex);
                    output.Statistics.RowsDeleted++;
                }
                else if (srcIndex == null && targetIndex.Status == "Deleted")
                {
                    targetIndex.Status = "Skipped";
                    output.SkippedRows.Add(targetIndex);
                    output.Statistics.RowsSkipped++;
                }
            }

            return output;
        }
    }
}
