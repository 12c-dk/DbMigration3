using SqlDapperClient.Managers;
using System.Collections;
using System.Text.Json;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using PropertyDescriptor = System.ComponentModel.PropertyDescriptor;
using Xunit.Abstractions;
using DbMigration.Common.Legacy.Helpers;
using DbMigration.Common.Legacy.Model.MappingModel;
//using AzureFunctions.Api.IntegrationTest.MappingModelTest;

//using Newtonsoft.Json.Linq;
//using Newtonsoft.Json;

namespace AzureFunctions.Api.IntegrationTest
{
    public class DapperIntTests
    {
        readonly ITestOutputHelper _output;

        readonly string _user = "sa";
        readonly string _password = "quoosugTh5";
        readonly SqlConnectionWrapper _sqlConnection;

        public DapperIntTests(ITestOutputHelper output)
        {
            _output = output;
            var conStr = $"Server=localhost,6000;Database=SourceDb;User Id={_user};Password={_password};TrustServerCertificate=True;Connect Timeout=2";
            _sqlConnection = new SqlConnectionWrapper(conStr);
        }

        [Fact]
        public async Task ConnectionTest()
        {

            var result = await _sqlConnection.QueryAsync("select top 10 * from SourceTable");

            List<dynamic> outputList = result.ToList();
            Assert.True(outputList.Count == 3);

            //Save output for testing
            //var test = JsonSerializer.Serialize(outputList, _jsonOptions);
            //await File.WriteAllTextAsync("PersonAddress.json", test);

        }

        [Fact]
        public void JsonToDictionaryTest()
        {
            string loadedFromFile = File.ReadAllText("Samples/PersonAddress.json");
            IEnumerable<dynamic> loadedDynamic = JsonSerializer.Deserialize<IEnumerable<dynamic>>(loadedFromFile);
            Assert.NotNull(loadedDynamic);

            var list = JsonHelper.JsonListToEnumerableList(loadedFromFile);

            Assert.NotNull(list);

            //Get specific value
            var outputValue = ((list[0]["MyArray"] as List<object>)?[4] as Dictionary<string, object>)?["SubArrayCity"];
            Debug.Assert(outputValue != null, nameof(outputValue) + " != null");
            var outputType = outputValue.GetType();
            Assert.NotNull(outputType);

        }

        [Fact]
        public void PopulateDbModelFromJsonTest()
        {
            //Todo: Solve problem - types are resolved incorrectly
            DbDatabaseRepository dbRepo = new DbDatabaseRepository();
            var database = dbRepo.EnsureDatabase("PopulateDbModelFromJson");
            DbTableSchema tableSchema = database.EnsureDbTableSchema("TestTable");

            string loadedFromFile = File.ReadAllText("Samples/PersonAddress.json");
            IEnumerable<dynamic> loadedDynamic = JsonSerializer.Deserialize<IEnumerable<dynamic>>(loadedFromFile);
            Assert.NotNull(loadedDynamic);


            //This works nicely
            var list = JsonHelper.JsonListToEnumerableList(loadedFromFile);
            Assert.NotNull(list);

            DbData data = JsonHelper.JsonListToDbData(tableSchema.SchemaId, loadedFromFile);

            DbTableSchema schema = new DbTableSchema(tableSchema.SchemaId, database);
            schema.LoadFromDbData(data);

            foreach (var field in schema.FieldsReadOnly)
            {
                _output.WriteLine($"{field.Name} - {field.FieldType}");
            }

            dbRepo.SaveDatabase(database);

            //Get specific value
            var outputValue = ((list[0]["MyArray"] as List<object>)?[4] as Dictionary<string, object>)?["SubArrayCity"];
            Debug.Assert(outputValue != null, nameof(outputValue) + " != null");
            var outputType = outputValue.GetType();
            Assert.NotNull(outputType);


        }

        [Fact]
        public void SimpleConvertTest()
        {
            string loadedFromFile = File.ReadAllText("Samples/SimpleJson.json");
            IEnumerable<dynamic> loadedDynamic = JsonSerializer.Deserialize<IEnumerable<dynamic>>(loadedFromFile);
            dynamic loadedDynamic2 = JsonSerializer.Deserialize<dynamic>(loadedFromFile);
            Assert.NotNull(loadedDynamic);
            Assert.NotNull(loadedDynamic2);


            var item1 = loadedDynamic.First();
            var addressId = item1.GetProperty("AddressID").GetInt32();
            Assert.Equal(1,addressId);
            var addressLine1 = item1.GetProperty("AddressLine1").ToString();
            Assert.Equal("1970 Napa Ct.", addressLine1);

            dynamic test3 = new ExpandoObject();
            test3.Name = "Hej";
            test3.ids = 42;

            IDictionary<string, object> dict = test3;
            Assert.Equal("Hej", dict["Name"]);
            Assert.Equal(42, dict["ids"]);

        }

        public Dictionary<String, Object> Dyn2Dict(dynamic dynObj)
        {
            var dictionary = new Dictionary<string, object>();

            var attributes = TypeDescriptor.GetAttributes(dynObj);
            var props = TypeDescriptor.GetProperties(dynObj);
            var propProps = TypeDescriptor.GetProperties(props);

            Assert.NotNull(attributes);
            Assert.NotNull(propProps);

            foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties(dynObj))
            {

                object obj = propertyDescriptor.GetValue(dynObj);
                var subProperties = TypeDescriptor.GetProperties(obj ?? throw new InvalidOperationException());
                Assert.NotNull(subProperties);
                dictionary.Add(propertyDescriptor.Name, obj);
            }

            return dictionary;
        }

        [Fact]
        public void JsonToHashtableTest()
        {
            string loadedFromFile = File.ReadAllText("Samples/SimpleObject.json");

            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonHashtableConverter());
            var hashTable = JsonSerializer.Deserialize<Hashtable>(loadedFromFile, options);
            Assert.IsType<Hashtable>(hashTable);
            var spartial = hashTable["SpatialLocation"];
            Assert.IsType<Hashtable>(spartial);
        }


        [Fact]
        public void DbSchemaRepositoryTest()
        {
            DbDatabaseRepository dbRepo = new DbDatabaseRepository();
            var database = dbRepo.EnsureDatabase("DbSchemaRepositoryDb");

            DbTableSchema schema = dbRepo.Tables.AddTableSchema(database, "TestSchema");

            var newSchemaId = schema.SchemaId;

            schema.EnsureTableField("Id,", TypeCode.Int64);
            var retrievedSchema = database.GetDbTableSchema(newSchemaId);
            Assert.NotNull(retrievedSchema);
        }

        [Fact]
        public void DbSchemaHierarchyTest()
        {
            //Todo: Setup a test on the hierarchy: DbCollection > DbSchema > DbData

            DbDatabaseRepository dbRepo = new DbDatabaseRepository();
                
            DbDatabase database  = dbRepo.EnsureDatabase("TestDatabase");
                
            
            database.EnsureDbTableSchema("TestTable");
            //getting database by name with different casing
            DbTableSchema tableExists = database.GetDbTableSchemaByTableName("testTable");
            tableExists.EnsureTableField("Id", TypeCode.Int64);

            Assert.NotNull(tableExists);

            //Following needs to go? Check if JsonDataManager is adequate for file backing. Otherwise get inspiration from DbSchemaOverviewHandler
            //DbSchemaOverviewHandler

            //List<string> tableNames = database.GetTableNames();
            dbRepo.SaveDatabase(database);

            List<string> tableNamesFileBacked = database.GetTableNames();
            Assert.Contains(tableNamesFileBacked, l => l.Equals("TestTable"));

            database.EnsureDbTableSchema("Test2");
            dbRepo.SaveDatabase(database);


        }

        [Fact]
        public void DbDatabaseRepositoryTest()
        {
            DbDatabaseRepository dbRepo = new DbDatabaseRepository();
            
            dbRepo.EnsureDatabase("TestDatabase2");

            var db =  dbRepo.GetDbDatabaseFromName("TestDatabase2");
            Assert.NotNull(db);
            dbRepo.SaveDatabase(db);

            var newTable = db.EnsureDbTableSchema("TestTable4");

            dbRepo.SaveDatabase(db);

            newTable.EnsureTableField("TestField", TypeCode.Decimal);
            dbRepo.SaveDatabase(db);

        }
            

        [Fact]
        public void SerializeTest()
        {
            DbDatabaseRepository dbRepo = new DbDatabaseRepository();
            DbDatabase db = dbRepo.EnsureDatabase("SerializeTestDatabase");

            db.EnsureDbTableSchema("test3");
            string json3 = JsonSerializer.Serialize(db, new JsonSerializerOptions { WriteIndented = true });
            Assert.NotNull(json3);

            dbRepo.SaveDatabase(db);

            Dictionary<Guid, DbTableSchema> dbTableSchemas = new Dictionary<Guid, DbTableSchema>
            {
                {Guid.NewGuid(), new DbTableSchema() { TableName = "TestTable" }}
            };
            string json4 = JsonSerializer.Serialize(dbTableSchemas, new JsonSerializerOptions { WriteIndented = true });
            Assert.NotNull(json4);

            Dictionary<Guid, string> dict3 = new Dictionary<Guid, string> {{Guid.NewGuid(), "Test3"}};
            string json5 = JsonSerializer.Serialize(dict3, new JsonSerializerOptions { WriteIndented = true });
            Assert.NotNull(json5);
        }

        



    }
}
