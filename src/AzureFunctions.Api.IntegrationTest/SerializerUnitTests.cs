using Microsoft.WindowsAzure.Storage.Table;
using System.Data;
using System.Text.Json;
using DbMigration.Common.Legacy.Helpers;
using DbMigration.Common.Legacy.Model.MappingModel;
using DbMigration.Common.Legacy.Model.Serialization;

namespace AzureFunctions.Api.IntegrationTest
{
    public class SerializerUnitTests
    {
        [Fact]
        public void DeserializeDyanmicTableEntityTest()
        {
            List<DynamicTableEntity> inputList = new List<DynamicTableEntity>();

            string pk1 = "PK-R1PZK";
            string pk2 = "PK-P2RZK";


            DynamicTableEntity dte = new DynamicTableEntity(pk1, "0746231f-cff9-4294-b99f-38118610d6da");
            dte.Properties.Add(new KeyValuePair<string, EntityProperty>("prop1", new EntityProperty("value1")));
            inputList.Add(dte);

            DynamicTableEntity dte2 = new DynamicTableEntity(pk2, "AnotherOne");
            dte2.Properties.Add(new KeyValuePair<string, EntityProperty>("prop1", new EntityProperty("value2")));
            inputList.Add(dte2);


            var serializedArray2 = TableEntitySerializer.Serialize(inputList);

            Assert.True(serializedArray2.Contains(pk1) &&
                        serializedArray2.Contains(pk2));

            List<DynamicTableEntity> output = TableEntitySerializer.Deserialize(serializedArray2);

            Assert.Contains(output, e => e.PartitionKey == pk1);
            Assert.Contains(output, e => e.PartitionKey == pk2);
        }

        [Fact]
        public void SerializeSystemTextTest()
        {
            DynamicTableEntity entity = new DynamicTableEntity("partitionKey", "rowKey");
            entity.Properties.Add("Property1", new EntityProperty("Value1"));
            entity.Properties.Add("Property2", new EntityProperty("Value2"));

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.Converters.Add(new MsEntityPropertyConverter());

            string serializedEntity = JsonSerializer.Serialize(entity, options);
            Assert.True(!string.IsNullOrEmpty(serializedEntity));

        }

        [Fact]
        public void SerializeEnumTypeTest()
        {
            DbTableSchema schema = new DbTableSchema();
            //DbField dbField = new DbField("Project", EdmType.Int64);
            schema.EnsureTableField("StorageTableStringField", EdmType.String);
            schema.EnsureTableField("Project", EdmType.Int64);
            schema.EnsureTableField(new DbField("SqlNVarCharField", SqlDbType.NVarChar));
            schema.EnsureTableField(new DbField("DotNetDoubleField", TypeCode.Double));
            schema.SchemaId = Guid.NewGuid();
            schema.DatabaseName = "TestDatabase";

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
                , Converters = { new InternalPropertiesConverter<DbTableSchema>() }
            };

            string jsonString = JsonSerializer.Serialize(schema, options);
            var deserialized = JsonSerializer.Deserialize<DbTableSchema>(jsonString, options);


            Assert.True(deserialized != null, nameof(deserialized) + " != null");
            Assert.True(deserialized.FieldsReadOnly.First(f => f.Name == "StorageTableStringField").FieldType.GetType() == typeof(EdmType));
            Assert.True(deserialized.FieldsReadOnly.First(f => f.Name == "SqlNVarCharField").FieldType.GetType() == typeof(SqlDbType));
            Assert.True(deserialized.FieldsReadOnly.First(f => f.Name == "DotNetDoubleField").FieldType.GetType() == typeof(TypeCode));

            Assert.True(deserialized.SchemaId != Guid.Empty);

        }

        [Fact]
        public void SerializeInternalPropertiesDbDatabaseTest()
        {
            DbDatabaseRepository dbRepo = new DbDatabaseRepository();

            DbDatabase db = dbRepo.EnsureDatabase("SerializeInternalProperties2TestDb");
            DbTableSchema schema = db.EnsureDbTableSchema("TestDb");
            schema.Config.TableAllowDuplicateNames = true;

            //DbField dbField = new DbField("Project", EdmType.Int64);
            schema.EnsureTableField("StorageTableStringField", EdmType.String);
            schema.EnsureTableField("Project", EdmType.Int64);
            schema.EnsureTableField("Project", EdmType.String);
            schema.EnsureTableField(new DbField("SqlNVarCharField", SqlDbType.NVarChar));
            schema.EnsureTableField(new DbField("DotNetDoubleField", TypeCode.Double));
            schema.SchemaId = Guid.NewGuid();
            schema.DatabaseName = "TestDatabase";

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
                , Converters =
                {
                    new InternalPropertiesConverter<DbDatabase>(),
                    new InternalPropertiesConverter<DbTableSchema>()
                }
            };

            string jsonString = JsonSerializer.Serialize(db, options);
            var deserialized = JsonSerializer.Deserialize<DbDatabase>(jsonString, options);

            Assert.True(deserialized.GetDbTableSchemaByTableName("TestDb").SchemaId == schema.SchemaId);
        }


    }
}
