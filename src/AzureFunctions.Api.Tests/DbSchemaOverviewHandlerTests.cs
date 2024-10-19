using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using DbMigration.Common.Legacy.Model.General;
using DbMigration.Common.Legacy.Model.MappingModel;
using DbMigration.Common.Legacy.Model.Serialization;
using Xunit;

namespace AzureFunctions.Api.Tests
{
    public class DbSchemaOverviewHandlerTests
    {

        [Fact]
        public void ModelOverviewTest()
        {
            DbSchemaOverviewHandler overview = new DbSchemaOverviewHandler();
            overview.AddOrUpdate(Guid.NewGuid(), "table1");
            //_schemaOverviewHandler.SaveOverviewToFile();

        }

        [Fact]
        public void LoadOverviewFromFile_ExistingFile_LoadsOverview()
        {
            // Arrange
            Guid schemaId = Guid.NewGuid();
            string json = $@"{{""{schemaId}"":""table1"" }}";
            File.WriteAllText("schemaOverview.json", json);
            DbSchemaOverviewHandler overview = new DbSchemaOverviewHandler();

            // Act
            overview.AddOrUpdate(Guid.NewGuid(), "table2");

            // Assert
            bool getSuccess =overview.TryGetValue(schemaId, out string outVar);
            Assert.True(getSuccess);
            Assert.Equal("table1", outVar);

            //Cleanup
            overview.Remove(schemaId);

        }

        [Fact]
        public void AddSchemaOverview_ValidSchema_AddsSchemaOverview()
        {
            // Arrange
            Guid schemaId = Guid.NewGuid();
            string tableName = "table1";

            // Act
            DbSchemaOverviewHandler overview = new DbSchemaOverviewHandler();
            overview.AddOrUpdate(schemaId, tableName);

            // Assert
            //var outTableName = _schemaOverviewHandler.GetTableName(schemaId);
            bool getSuccess = overview.TryGetValue(schemaId, out string outTableName);
            Assert.True(getSuccess);
            Assert.Equal(tableName, outTableName);
        }

        [Fact]
        public void RemoveSchemaOverview_ValidSchema_RemovesSchemaOverview()
        {
            // Arrange
            Guid schemaId = Guid.NewGuid();

            // Act
            DbSchemaOverviewHandler overview = new DbSchemaOverviewHandler();
            overview.AddOrUpdate(schemaId, "TableToBeRemoved");
            bool getSuccess = overview.TryGetValue(schemaId, out string _);
            Assert.True(getSuccess);

            overview.Remove(schemaId);

            // Assert
            bool getSuccess2 = overview.TryGetValue(schemaId, out string _);
            Assert.False(getSuccess2);

        }


        [Fact]
        public void JsonDictionaryManagerTest()
        {
            string fileName = "jsonSchema.json";
            if (File.Exists(fileName)) File.Delete(fileName);
            
            JsonDictionaryManager mgr = new JsonDictionaryManager("jsonSchema.json");
            var allItems = mgr.GetAll();
            Assert.True(allItems.Count == 0);

            string key = "key1";
            mgr.AddOrUpdate(key, "value1");
            mgr.AddOrUpdate(key, "value2");
            Assert.True(mgr.GetAll().Count == 1);

            
            
            string key2 = "key2";
            int value = 42;
            mgr.AddOrUpdate(key2, value);
            Assert.True(mgr.GetAll().Count == 2);
            
            mgr.TryGetValue(key2, out object outValue);
            Assert.True(Convert.ToInt16(outValue) == value);

        }

        [Fact]
        public void JsonDictionaryManagerSchemaExistsTest()
        {
            string content = "{\r\n  \"key1\": \"value2\",\r\n  \"key2\": 42\r\n}";
            string fileName = "jsonSchema.json";
            if (File.Exists(fileName)) File.Delete(fileName);

            File.WriteAllText(fileName, content);

            JsonDictionaryManager mgr = new JsonDictionaryManager("jsonSchema.json");
            var allItems = mgr.GetAll();
            Assert.True(allItems.Count == 2);

            string key = "key3";
            mgr.AddOrUpdate(key, "value1");
            mgr.AddOrUpdate(key, "value2");
            Assert.True(mgr.GetAll().Count == 3);

            string key2 = "key4";
            int value = 84;
            mgr.AddOrUpdate(key2, value);
            Assert.True(mgr.GetAll().Count == 4);

            mgr.Remove(key);
            Assert.True(mgr.GetAll().Count == 3);

            mgr.TryGetValue(key2, out object outValue);
            Assert.True(Convert.ToInt16(outValue) == value);

        }

        [Fact]
        public void JsonDataManagerTest()
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
            
            JsonDataManager<DbTableSchema> mgr = new JsonDataManager<DbTableSchema>("TestData.json", serializerOptions: options);

            DbTableSchema schema = new DbTableSchema
            {
                TableName = "TestTable"
            };
            schema.EnsureTableField(new DbField("TestField", SqlDbType.Binary){IsPrimaryKey = true});
            schema.EnsureTableField(new DbField("TestField2", TypeCode.Int16){IsPrimaryKey = true});
            
            mgr.Data = schema;
            mgr.SaveData();

            string tableName = mgr.Data.TableName;
            
            Assert.Equal(tableName, schema.TableName);

            var type = mgr.Data.GetFieldNameExists("TestField")?.FieldType;
            Debug.Assert(type != null, nameof(type) + " != null");
            var typeName = type.GetType().FullName;
            Assert.NotNull(typeName);

            var type2 = mgr.Data.GetFieldNameExists("TestField2")?.FieldType;
            Debug.Assert(type2 != null, nameof(type2) + " != null");
            var typeName2 = type2.GetType().FullName;
            Assert.NotNull(typeName2);
        }

        [Fact]
        public void JsonDataManager2Test()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters =
                {
                    new InternalPropertiesConverter<DbDatabase>(),
                    new InternalPropertiesConverter<DbTableSchema>()
                }
            };
            
            JsonDataManager<DbTableSchema> mgr = new JsonDataManager<DbTableSchema>("JsonDataManager2Data.json", serializerOptions: options);

            mgr.Data.TableName = "NewTableName";

            mgr.Data.EnsureTableField(new DbField("TestName2", TypeCode.String));

            mgr.SaveData();

            mgr.Data.TableName = "NewTableName";
            //Here the Save doesn't write to file, since there is no change in the json.
            mgr.SaveData();


        }

        [Fact]
        public void JsonDataManagerManualLoadingTest()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters =
                {
                    new InternalPropertiesConverter<DbDatabase>(),
                    new InternalPropertiesConverter<DbTableSchema>()
                }
            };
            
            File.Delete("TestData.json");
            JsonDataManager<DbTableSchema> mgr = new JsonDataManager<DbTableSchema>("TestData.json", true, serializerOptions: options);

            //When manualLoading == true - Can access object multiple times without json file is loaed
            mgr.Data.TableName = "NewTableName3";
            mgr.Data.EnsureTableField(new DbField("TestName2", TypeCode.String));
            mgr.Data.TableName = "NewTableName";
            mgr.SaveData();

        }

    }
}
