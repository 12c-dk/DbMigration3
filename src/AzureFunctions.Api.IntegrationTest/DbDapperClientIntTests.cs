using AzureFunctions.Api.IntegrationTest.Mocks;
using DbMigration.Common.Legacy.Model.MappingModel;
using SqlDapperClient.Managers;
using Xunit.Abstractions;

namespace AzureFunctions.Api.IntegrationTest
{
    public class DbDapperClientIntTests
    {
        readonly ITestOutputHelper _output;

        readonly string _user = "sa";
        readonly string _password = "quoosugTh5";
        readonly SqlConnectionWrapper _sqlConnection;
        readonly MockLogger<DbDapperClient> _dbDapperClientLogger;
        public DbDapperClientIntTests(ITestOutputHelper output)
        {
            _output = output;
            var conStr = $"Server=localhost,6000;Database=SourceDb;User Id={_user};Password={_password};TrustServerCertificate=True;Connect Timeout=2";
            _sqlConnection = new SqlConnectionWrapper(conStr);
            _dbDapperClientLogger = new MockLogger<DbDapperClient>(output);

            _sqlConnection.ExecuteAsync(@"
IF NOT EXISTS (SELECT *
FROM INFORMATION_SCHEMA.TABLES
WHERE  TABLE_NAME = 'TestTable' AND TABLE_SCHEMA = 'dbo')
	CREATE TABLE TestTable
(
	[Id] [int] IDENTITY NOT NULL,
	Name VARCHAR (100) NULL, 
	Age  INT NULL
	PRIMARY KEY (Id)
) "
            ).Wait();

            _sqlConnection.ExecuteAsync(@"
IF NOT EXISTS (SELECT *
FROM INFORMATION_SCHEMA.TABLES
WHERE  TABLE_NAME = 'TestTableTwoKeys' AND TABLE_SCHEMA = 'dbo')
	CREATE TABLE TestTableTwoKeys
(
	[Id1] [int] IDENTITY NOT NULL,
	[Id2] [int] NOT NULL,
	Name VARCHAR (100) NULL, 
	Age  INT NULL
	PRIMARY KEY (Id1,Id2)
) "
            ).Wait();


        }


        [Fact]
        public async Task GetTablesTest()
        {
            DbDatabaseRepository dbRepo = new DbDatabaseRepository();
            DbDatabase db = dbRepo.EnsureDatabase("SerializeTestDatabase");


            DbDapperClient dbDapperClient = new DbDapperClient(_sqlConnection, db, _dbDapperClientLogger);
            var tables = await dbDapperClient.GetTables();
            Assert.True(tables.Count > 3);

            foreach (string table in tables)
            {
                _output.WriteLine(table);
            }
        }

        [Fact]
        public async Task GetDbSchemaTest()
        {
            DbDatabaseRepository dbRepo = new DbDatabaseRepository();
            var db = dbRepo.EnsureDatabase("GetDbSchema");
            DbDapperClient dapperHelper = new DbDapperClient(_sqlConnection, db, _dbDapperClientLogger);

            var schema = await dapperHelper.GetTableSchema("TestTableTwoKeys");

            Assert.True(schema.FieldsReadOnly.Count > 3);
        }

        [Fact(Skip = "Used for exploratory testing")]
        public async Task FindTableWithTwoPrimaryKeysTest()
        {

            DbDatabaseRepository dbRepo = new DbDatabaseRepository();
            var db = dbRepo.EnsureDatabase("FindTableWithTwoPrimaryKeys");

            DbDapperClient dbDapperClient = new DbDapperClient(_sqlConnection, db, _dbDapperClientLogger);

            var tables = await dbDapperClient.GetTables();
            Assert.True(tables.Count > 10);

            foreach (string table in tables)
            {
                var schema = await dbDapperClient.GetTableSchema(table);
                var primaryKeyFields = schema.FieldsReadOnly.Where(f => f.IsPrimaryKey).ToList();
                if (primaryKeyFields.Count > 1)
                {
                    _output.WriteLine($"Found table with two primary keys:  {schema.TableName} Primary keys: {primaryKeyFields.Count}");
                }
            }
        }


        [Fact]
        public async Task GetDataTest()
        {
            //This test loads data to a DbModel
            DbDatabaseRepository dbRepo = new DbDatabaseRepository();
            DbDatabase database = dbRepo.EnsureDatabase("GetDataTestDb");
            var tableSchema = database.EnsureDbTableSchema("SourceTable");


            DbDapperClient dapperClient = new DbDapperClient(_sqlConnection, database, _dbDapperClientLogger);

            DbData dbData = await dapperClient.GetTableData(tableSchema,
                top: 1000,
                selectFields: new List<string>() { "Name", "Age" },
                queryString: "Age > 190");
            Assert.True(dbData.Count == 2);

            _output.WriteLine(dbData.ToString(columns: new List<string>() { "Name", "Age" }, top: 100));

        }



        [Fact]
        public async Task InsertTest()
        {
            //Verify testtable:
            //sqlcmd -S localhost,6000 -U sa -P 'quoosugTh5' -Q "USE AdventureWorks2019;SELECT * from testtable"

            if (File.Exists("InsertTestDb.json"))
            {
                File.Delete("InsertTestDb.json");
            }

            DbDatabaseRepository dbRepo = new DbDatabaseRepository();
            DbDatabase database = dbRepo.EnsureDatabase("InsertTestDb");
            DbDapperClient dapperClient = new DbDapperClient(_sqlConnection, database, _dbDapperClientLogger);

            DbTableSchema extractedSchema = await dapperClient.GetTableSchema("TestTable");
            extractedSchema.Config.OnInsertFieldsNotInSchemaResponse = DbTableSchemaReaction.Error;

            database.AddDbTableSchema(extractedSchema);
            dbRepo.SaveDatabase(database);

            DbData data = new DbData(extractedSchema.SchemaId)
            {
                new DbItem(
                    new Dictionary<string, object>()
                    {
                        {"Name", "Lindboe"},
                        {"Age", 89},
                        //{"Id", 2004},
                        {"NonExistingField", 55}
                    }
                ),
                new DbItem(
                    new Dictionary<string, object>()
                    {
                        {"Name", "Andersen"},
                        {"Age", 101}
                    }
                ),
                new DbItem(
                    new Dictionary<string, object>()
                    {
                        {"Name", "Jensen"},
                        {"Age", 101},
                        {"Id", 2005}
                    }
                )
            };


            var insertResponse = await dapperClient.InsertRows("TestTable", data);
            _output.WriteLine(insertResponse.ToString());

            Assert.Contains(insertResponse.OperationResponse.ItemResponses, i => i.OutputMessage.Contains("Item contains fields that are not in schema: NonExistingField"));
            Assert.Contains(insertResponse.OperationResponse.ItemResponses, i => i.OutputMessage.Contains("Item contains Identity fields"));
            Assert.True(insertResponse.OperationResponse.OperationResult == OperationResultEnum.PartialSuccess, "OperationResult is not PartialSuccess");

            var andersenInsert = insertResponse.ResponseValue.Values.FirstOrDefault(i => i["Name"].ToString() == "Andersen");
            Assert.NotNull(andersenInsert);
            Assert.True(andersenInsert["Id"].ToString() != "2005");


        }

        [Fact]
        public async Task InsertFailNonExistingFieldTest()
        {
            //Verify testtable:
            //sqlcmd -S localhost,6000 -U sa -P 'quoosugTh5' -Q "USE AdventureWorks2019;SELECT * from testtable"

            if (File.Exists("InsertTestDb.json"))
            {
                File.Delete("InsertTestDb.json");
            }

            DbDatabaseRepository dbRepo = new DbDatabaseRepository();
            DbDatabase database = dbRepo.EnsureDatabase("InsertTestDb");
            DbDapperClient dapperClient = new DbDapperClient(_sqlConnection, database, _dbDapperClientLogger);

            DbTableSchema extractedSchema = await dapperClient.GetTableSchema("TestTable");
            extractedSchema.ParentDbDatabase = database;
            extractedSchema.Config.OnInsertFieldsNotInSchemaResponse = DbTableSchemaReaction.Ignore;

            var testSchema2 = database.EnsureDbTableSchema("TestTable");
            //todo: Find a better way to merge 
            foreach (var field in extractedSchema.FieldsReadOnly)
            {
                testSchema2.EnsureTableField(field);
            }

            dbRepo.SaveDatabase(database);

            DbItem item = new DbItem
            {
                {"Name", "Lindboe"},
                {"Age", 89},
                {"NonExistingField", 55}
            };

            DbData data = new DbData(extractedSchema.SchemaId) {item};

            var insertResponse = await dapperClient.InsertRows("TestTable", data);
            _output.WriteLine(insertResponse.ToString());


            Assert.True(insertResponse.OperationResponse.OperationResult == OperationResultEnum.Failure);
            Assert.Contains("Item contains fields that are not in schema: NonExistingField", insertResponse.OperationResponse.ItemResponses.First().OutputMessage);

        }

        [Fact]
        public async Task UpdateTest()
        {
            //Verify testtable:
            //sqlcmd -S localhost,6000 -U sa -P 'quoosugTh5' -Q "USE AdventureWorks2019;SELECT * from TestTableTwoKeys"

            if (File.Exists("InsertTestDb.json"))
            {
                File.Delete("InsertTestDb.json");
            }

            DbDatabaseRepository dbRepo = new DbDatabaseRepository();
            DbDatabase database = dbRepo.EnsureDatabase("InsertTestDb");
            DbDapperClient dapperClient = new DbDapperClient(_sqlConnection, database, _dbDapperClientLogger);


            DbTableSchema extractedSchema = await dapperClient.GetTableSchema("TestTableTwoKeys");
            extractedSchema.Config.OnInsertFieldsNotInSchemaResponse = DbTableSchemaReaction.Ignore;

            database.AddDbTableSchema(extractedSchema);
            dbRepo.SaveDatabase(database);



            DbData data = new DbData(extractedSchema.SchemaId)
            {
                new DbItem(
                    new Dictionary<string, object>()
                    {
                        { "Name", "Lindboe" },
                        { "Age", 153 },
                        { "Id1", 1 },
                        { "Id2", 2005 }
                    }
                ),
                new DbItem(
                    new Dictionary<string, object>()
                    {
                        {"Name","Andersen"},
                        {"Age", 71},
                        {"Id1", 2},
                        {"Id2", 1004}
                    }
                )
            };

            var (updatedData, operationResponse) = await dapperClient.UpdateRows("TestTableTwoKeys", data);
            Assert.NotNull(updatedData);

            _output.WriteLine(operationResponse.ToString());

            Assert.True(operationResponse.OperationResult == OperationResultEnum.PartialSuccess);
            Assert.True(operationResponse.SuccessItems.Count == 1);

        }

        [Fact]
        public async Task UpsertTest()
        {
            //Verify testtable:
            //sqlcmd -S localhost,6000 -U sa -P 'quoosugTh5' -Q "USE AdventureWorks2019;SELECT * from TestTableTwoKeys"

            if (File.Exists("InsertTestDb.json"))
            {
                File.Delete("InsertTestDb.json");
            }

            DbDatabaseRepository dbRepo = new DbDatabaseRepository();
            DbDatabase database = dbRepo.EnsureDatabase("InsertTestDb");
            DbDapperClient dapperClient = new DbDapperClient(_sqlConnection, database, _dbDapperClientLogger);


            DbTableSchema extractedSchema = await dapperClient.GetTableSchema("TestTableTwoKeys");
            extractedSchema.Config.OnInsertFieldsNotInSchemaResponse = DbTableSchemaReaction.Ignore;

            database.AddDbTableSchema(extractedSchema);
            dbRepo.SaveDatabase(database);



            DbData data = new DbData(extractedSchema.SchemaId)
            {
                new DbItem(
                    new Dictionary<string, object>()
                    {
                        { "Name", "Lindboe" },
                        { "Age", 150 },
                        { "Id1", 1 },
                        { "Id2", 2005 }
                    }
                ),
                new DbItem(
                    new Dictionary<string, object>()
                    {
                        {"Name","Andersen"},
                        {"Age", 70},
                        {"Id1", 2},
                        {"Id2", 1004}
                    }
                )
            };

            var upsertResponse = await dapperClient.UpsertRows("TestTableTwoKeys", data);
            _output.WriteLine(upsertResponse.ToString());

            Assert.True(upsertResponse.OperationResult == OperationResultEnum.Success);

        }

        [Fact]
        public async Task Insert2Test()
        {
            //Verify testtable:
            //sqlcmd -S localhost,6000 -U sa -P 'quoosugTh5' -Q "USE AdventureWorks2019;SELECT * from TestTableTwoKeys"

            if (File.Exists("InsertTestDb.json"))
            {
                File.Delete("InsertTestDb.json");
            }

            DbDatabaseRepository dbRepo = new DbDatabaseRepository();
            DbDatabase database = dbRepo.EnsureDatabase("InsertTestDb");
            DbDapperClient dapperClient = new DbDapperClient(_sqlConnection, database, _dbDapperClientLogger);


            DbTableSchema extractedSchema = await dapperClient.GetTableSchema("TestTableTwoKeys");
            extractedSchema.Config.OnInsertFieldsNotInSchemaResponse = DbTableSchemaReaction.Ignore;

            database.AddDbTableSchema(extractedSchema);
            dbRepo.SaveDatabase(database);


            DbData data = new DbData(extractedSchema.SchemaId)
            {
                new DbItem(
                    new Dictionary<string, object>()
                    {
                        {"Name","Lindboe"},
                        {"Age", 80},
                        {"Id1", 2005},
                        {"Id2", 2005}
                    }
                ),
                new DbItem(
                    new Dictionary<string, object>()
                    {
                        {"Name","Andersen"},
                        {"Age", 70},
                        {"Id1", 2004},
                        {"Id2", 2004}
                    }
                )
            };

            var insertResponse = await dapperClient.InsertRows("TestTableTwoKeys", data);
            _output.WriteLine(insertResponse.ToString());

            Assert.True(insertResponse.OperationResponse.OperationResult == OperationResultEnum.Success);
        }

        [Fact]
        public async Task AddSchemaToDatabaseTest()
        {
            //Verify testtable:
            //sqlcmd -S localhost,6000 -U sa -P 'quoosugTh5' -Q "USE AdventureWorks2019;SELECT * from testtable"

            if (File.Exists("InsertTestDb.json"))
            {
                File.Delete("InsertTestDb.json");
            }

            DbDatabaseRepository dbRepo = new DbDatabaseRepository();
            DbDatabase database = dbRepo.EnsureDatabase("InsertTestDb");
            DbDapperClient dapperClient = new DbDapperClient(_sqlConnection, database, _dbDapperClientLogger);

            DbTableSchema extractedSchema = await dapperClient.GetTableSchema("TestTable");
            extractedSchema.Config.OnInsertFieldsNotInSchemaResponse = DbTableSchemaReaction.Ignore;
            database.AddDbTableSchema(extractedSchema);
            dbRepo.SaveDatabase(database);

            Assert.True(database.Tables.FirstOrDefault(t => t.TableName == "TestTable") != null, "Database doesn't contain TestTable");
        }


        [Fact]
        public async Task GetItemsFromDbByIdSimpleTest()
        {
            //This test loads data to a DbModel
            DbDatabaseRepository dbRepo = new DbDatabaseRepository();
            DbDatabase database = dbRepo.EnsureDatabase("InsertTestDb");
            DbDapperClient dapperClient = new DbDapperClient(_sqlConnection, database, _dbDapperClientLogger);

            DbTableSchema tableTwokeysSchema = await dapperClient.GetTableSchema("TestTableTwoKeys");

            List<DbItem> inputIdentifiers = new List<DbItem>
            {
                new DbItem(
                    new Dictionary<string, object>()
                    {
                        {"Id1", 1},
                        {"Id2", 2005}
                    }
                ),
                new DbItem(
                    new Dictionary<string, object>()
                    {
                        {"Id1", 2},
                        {"Id2", 2004}
                    }
                ),
                new DbItem(
                    new Dictionary<string, object>()
                    {
                        {"Id1", 2},
                        {"Id2", 1004}
                    }
                ),
                new DbItem(
                    new Dictionary<string, object>()),
                null
            };

            var itemsFromIdResponse = await dapperClient.GetItemsFromDbById(tableTwokeysSchema, inputIdentifiers, new List<string>(){"Name", "Id2", "Id1"});
            List<DbItem> data = itemsFromIdResponse.ResponseValue;
            List<DbItem> failedItems = itemsFromIdResponse.NoMatchItems;

            Assert.NotNull(data);
            Assert.True(data.Count == 2);
            Assert.True(failedItems.Count == 1);

            var firstItem = data.First(i => i["Id2"].ToString() == "2005");
            Assert.True(firstItem["Id1"].ToString() == "1");
            


        }

    }
}
