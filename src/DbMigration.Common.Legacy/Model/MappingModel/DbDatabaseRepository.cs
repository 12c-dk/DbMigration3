using System.Text.Json;
using DbMigration.Common.Legacy.Model.Serialization;

namespace DbMigration.Common.Legacy.Model.MappingModel
{
    public class DbDatabaseRepository
    {
        public readonly DbTableSchemaRepository Tables;
        public DbDatabaseRepository()
        {
            Tables = new DbTableSchemaRepository(this);
        }


        public DbDatabase EnsureDatabase(string databaseName)
        {
            var existingDb = GetDbDatabaseFromName(databaseName);
            if (existingDb != null)
            {
                return existingDb;
            }

            DbDatabase database = new DbDatabase(this, databaseName);
            database.Validate();
            SaveDatabase(database);
            return database;
        }

        public DbDatabase GetDbDatabaseFromName(string name)
        {
            string filePath = $"{name.ToLower()}.json";

            if (!File.Exists(filePath))
                return null;

            string json = File.ReadAllText(filePath);

            if (string.IsNullOrEmpty(json))
                return null;


            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters =
                {
                    new InternalPropertiesConverter<DbDatabase>(),
                    new InternalPropertiesConverter<DbTableSchema>()
                }
            };
            DbDatabase database = JsonSerializer.Deserialize<DbDatabase>(json, options);


            if (database == null)
                return null;

            database.InitializeAfterDeserialization(this);

            if (string.IsNullOrEmpty(database.DatabaseName))
                database.DatabaseName = name.ToLower();

            database.Validate();

            return database;

        }

        public void SaveDatabase(DbDatabase database)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
                ,
                Converters =
                {
                    new InternalPropertiesConverter<DbDatabase>(),
                    new InternalPropertiesConverter<DbTableSchema>()
                }
            };

            database.Validate();

            string json = JsonSerializer.Serialize(database, options);
            File.WriteAllText($"{database.DatabaseName}.json", json);

            foreach (var table in database.Tables)
            {
                Tables.SaveDbTableSchema(table);

            }
        }
    }
}
