namespace DbMigration.Common.Legacy.Model.MappingModel
{
    public class DbTableSchemaConfig
    {
        public bool TableAllowDuplicateNames { get; set; }

        public DbTableSchemaReaction OnInsertFieldsNotInSchemaResponse { get; set; } = DbTableSchemaReaction.Error;
    }

    public enum DbTableSchemaReaction
    {
        Error = 1,
        Ignore = 2,
        Include = 3
    }
}
