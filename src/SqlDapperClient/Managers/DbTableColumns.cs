using System.Diagnostics.CodeAnalysis;

namespace SqlDapperClient.Managers
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class DbTableColumns
    {
        public string TABLE_NAME { get; set; }
        public string COLUMN_NAME { get; set; }
        public string DATA_TYPE { get; set; }
        public int? CHARACTER_MAXIMUM_LENGTH { get; set; }
        public string IS_NULLABLE { get; set; }
        public bool PrimaryKey { get; set; }
        public string COLUMN_DEFAULT { get; set; }

        /// <summary>
        /// On sql table, IsIdentity indicates that Identity specification (Autonumbering) is enabled and that values cannot be inserted into the column.
        /// </summary>
        public bool IsIdentity { get; set; }
        
    }
}
