using Newtonsoft.Json;
using Guid = System.Guid;

namespace DbMigration.Common.Legacy.Model.MappingModel
{
    public class DbDatabase
    {
        public string DatabaseName { get; set; }
        //We can call Save every time Tables get is called

        /// <summary>
        /// Key: SchemaId (Guid)
        /// </summary>
        /// Needs to be public for serialization
        public List<DbTableSchema> Tables { get; set; }

        public DbDatabaseConfig Config { get; set; } = new DbDatabaseConfig();

        [JsonIgnore]
        internal DbDatabaseRepository DatabaseRepository;

        //Used for serialization / deserialization
        public DbDatabase()
        {
            Tables = new List<DbTableSchema>();
        }

        //Used for Initializing new instance and from EnsureDatabase
        public DbDatabase(DbDatabaseRepository databaseRepository, string databaseName)
        {
            DatabaseName = databaseName.ToLower();
            DatabaseRepository = databaseRepository;

            Tables = new List<DbTableSchema>();
        }

        public void InitializeAfterDeserialization(DbDatabaseRepository databaseRepository)
        {
            DatabaseRepository = databaseRepository;
            foreach (var table in Tables)
            {
                table.ParentDbDatabase = this;
            }
        }


        public void Validate()
        {
            if (string.IsNullOrEmpty(DatabaseName))
            {
                throw new ApplicationException("Schema validation failed. _databaseName is not set");
            }

            if (DatabaseName.ToLower() != DatabaseName)
            {
                throw new ApplicationException("DatabaseName contains uppercase letters. This is not allowed. ");
            }

            if (DatabaseRepository == null)
            {
                throw new ApplicationException("Schema validation failed. DatabaseRepository is not set");
            }

        }

        // ## Original DbTableCollection functionality moved to DbDatabase

        public DbTableSchema GetDbTableSchema(Guid schemaId)
        {
            return Tables.FirstOrDefault(t => t.SchemaId == schemaId);
        }

        public DbTableSchema GetDbTableSchemaByTableName(string tableName)
        {
            DbTableSchema table = Tables.FirstOrDefault(t => t.TableName.ToLower() == tableName.ToLower());
            return table;

        }

        public void AddDbTableSchema(DbTableSchema schema)
        {
            //Validate tableSchema
            var existingTable = GetDbTableSchemaByTableName(schema.TableName);
            if (existingTable != null)
            {
                throw new ApplicationException($"AddDbTableSchema called with tableName {schema.TableName}, but a table by this name already exist in this database.");
            }

            if (schema.SchemaId == Guid.Empty)
            {
                schema.SchemaId = Guid.NewGuid();
            }
            schema.ParentDbDatabase = this;

            Tables.Add(schema);

        }

        public DbTableSchema EnsureDbTableSchema(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentNullException(nameof(tableName));
            }

            var existingTable = GetDbTableSchemaByTableName(tableName);
            if (existingTable != null)
            {
                return existingTable;
            }

            //Table doesn't exist. Create table
            Guid schemaId = Guid.NewGuid();

            DbTableSchema newSchema = new DbTableSchema(schemaId, this)
            {
                TableName = tableName
            };
            newSchema.Validate();

            Tables.Add(newSchema);
            DatabaseRepository.SaveDatabase(this);
            return newSchema;

        }

        public List<string> GetTableNames()
        {
            return Tables.Select(s => s.TableName).ToList();
        }

    }
}
