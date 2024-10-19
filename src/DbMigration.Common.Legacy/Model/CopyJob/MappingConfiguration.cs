namespace DbMigration.Common.Legacy.Model.CopyJob
{
    public class MappingConfiguration
    {
        public bool CopyUnmappedTables { get; set; } = false;
        //Columns to copy from unmapped tables

        public List<TableMapping> TableMappings { get; set; } = new List<TableMapping>();

    }
}
