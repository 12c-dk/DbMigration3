namespace DbMigration.Common.Legacy.Model.MappingModel
{
    public class DbDatabaseConfig
    {
        //This property is obsolete since TableAllowDuplicateNames has been added to DbTableSchemaConfig
        public bool TableAllowDuplicateNames { get; set; } = false;
    }
}
