using System.Text.Json;

namespace DbMigration.Common.Legacy.Model.MappingModel
{
    public class DbTableSchemaRepository
    {
        public DbDatabaseRepository DatabaseRepository { get; set; }

        public DbTableSchemaRepository(DbDatabaseRepository databaseRepository)
        {
            DatabaseRepository = databaseRepository;
        }


        public DbTableSchema GetDbTableSchemaFromId(Guid id)
        {
            string filePath = $"{id}.json";
            if (!File.Exists(filePath))
                return null;

            string json = File.ReadAllText(filePath);

            if (string.IsNullOrEmpty(json))
                return null;

            DbTableSchema tableSchema = JsonSerializer.Deserialize<DbTableSchema>(json);

            if (tableSchema == null)
                return null;

            tableSchema.Validate();

            return tableSchema;
        }

        public void SaveDbTableSchema(DbTableSchema tableSchema)
        {
            tableSchema.Validate();
            string filePath = $"{tableSchema.SchemaId}.json";
            string json = JsonSerializer.Serialize(tableSchema, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public DbTableSchema AddTableSchema(DbDatabase database, string tableName)
        {
            DbTableSchema schema = new DbTableSchema(Guid.NewGuid(), database)
            {
                TableName = tableName
            };
            schema.Validate();

            database.Tables.Add(schema);
            database.DatabaseRepository.SaveDatabase(database);
            return schema;

        }



    }
}
