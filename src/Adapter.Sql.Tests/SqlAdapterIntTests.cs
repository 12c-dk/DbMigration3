using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using DbMigration.Domain.DictionaryModels;
using DbMigration.Domain.Model;
using DbMigration.Sync.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Adapter.Sql.Tests
{
    public class SqlAdapterIntTests
    {
        //private readonly SynchronizationModule _synchronizationModule;
        private readonly SqlAdapter _sqlAdapter;
        private readonly ITestOutputHelper _iTestOutputHelper;
        private readonly string _srcConStr;

        public SqlAdapterIntTests(ITestOutputHelper iTestOutputHelper)
        {
            _iTestOutputHelper = iTestOutputHelper;
            ConfigManager configManager = new ConfigManager();
            _srcConStr = configManager.GetConfigValue("SourceConnectionString");

            // Set up the DI container
            ServiceCollection serviceCollection = new ServiceCollection();

            //serviceCollection.AddTransient<IAdapter, SqlAdapter>();
            serviceCollection.AddTransient<SqlAdapter>();

            serviceCollection.AddLogging();

            // Build the service provider
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Resolve and use the synchronization module
            //_synchronizationModule = serviceProvider.GetRequiredService<SynchronizationModule>();
            _sqlAdapter = serviceProvider.GetRequiredService<SqlAdapter>();
        }

        [Fact]
        public async Task GetTablesTest()
        {
            var srcConfig = new SqlAdapterConfig { ConnectionString = _srcConStr };
            Debug.Assert(_sqlAdapter != null, nameof(_sqlAdapter) + " != null");
            var configResult = await _sqlAdapter.SetConfiguration(srcConfig);
            Assert.True(configResult.IsOk, configResult.ToString());
            
            var tables = await _sqlAdapter.GetTables();

            foreach (var table in tables)
            {
                _iTestOutputHelper.WriteLine(table);
            }

            Assert.True(tables.Count > 0);

        }

        [Fact]
        public async Task InsertTest()
        {
            string tableName = "TargetTable";
            var srcConfig = new SqlAdapterConfig { ConnectionString = _srcConStr };
            Assert.NotNull(_sqlAdapter);
            var configResult = await _sqlAdapter.SetConfiguration(srcConfig);
            Assert.True(configResult.IsOk, configResult.ToString());

            List<DbItem> data = new List<DbItem>()
            {
                new DbItem()
                {
                    Data = new DictionaryCaseInsensitive<object>()
                    {
                        {"Name", "Inserted name"}
                    },
                    Identifiers = new DictionaryCaseInsensitive<object>()
                    {
                        {"Id", 100}
                    }
                }
            };

            Debug.Assert(_sqlAdapter != null, nameof(_sqlAdapter) + " != null");
            var result = await _sqlAdapter.InsertRows(tableName, data);
            Assert.NotNull(result);
            Assert.True(result.IsOk, result.ToString());

            var matches = await _sqlAdapter.GetTableData(tableName, queryString: "Name = 'Inserted Name'");
            Assert.True(matches.Count > 0);

            await _sqlAdapter.DeleteItems(tableName, data);

        }

        [Fact]
        public async Task UpdateTest()
        {
            string tableName = "TargetTable";
            var srcConfig = new SqlAdapterConfig { ConnectionString = _srcConStr };
            var configResult = await _sqlAdapter.SetConfiguration(srcConfig);
            Assert.True(configResult.IsOk, configResult.ToString());

            List<DbItem> data = new List<DbItem>
            {
                new DbItem
                {
                    Identifiers = new DictionaryCaseInsensitive<object>
                    {
                        {"Id", 120}
                    },
                    Data =
                    {
                        {"Name", "Inserted name 120"}
                    }
                }
            };

            try
            {
                var insertResult = await _sqlAdapter.InsertRows(tableName, data);
                Assert.NotNull(insertResult);
                Assert.True(insertResult.OperationResult == OperationResultEnum.Success, "Insert operation failed");

                List<DbItem> items = new List<DbItem>
                {
                    new DbItem
                    {
                        Data = new DictionaryCaseInsensitive<object>
                        {
                            {"Name", "Updated name2"}
                        },
                        Identifiers = new DictionaryCaseInsensitive<object>
                        {
                            {"Id", 120}
                        }
                    }
                };

                var updateResult = await _sqlAdapter.UpdateRows(tableName, items);
                Assert.NotNull(updateResult);
                Assert.True(updateResult.OperationResponse.IsOk, updateResult.OperationResponse.ToString());

                var matches = await _sqlAdapter.GetTableData(tableName, queryString: "Id = 120");
                Assert.True(matches.Count > 0);
                Assert.Equal("Updated name2", matches[0].Data["Name"]);
            }
            finally
            {
                // Cleanup: Delete the inserted row
                List<DbItem> deleteItems = new List<DbItem>
                {
                    new DbItem
                    {
                        Identifiers = new DictionaryCaseInsensitive<object>
                        {
                            {"Id", 120}
                        }
                    }
                };
                await _sqlAdapter.DeleteItems(tableName, deleteItems);
            }
        }

        [Fact]
        public async Task DeleteTest()
        {
            string tableName = "TargetTable";
            var srcConfig = new SqlAdapterConfig { ConnectionString = _srcConStr };
            var configResult = await _sqlAdapter.SetConfiguration(srcConfig);
            Assert.True(configResult.IsOk, configResult.ToString());

            List<DbItem> data = new List<DbItem>()
            {
                new DbItem()
                {
                    Data = new DictionaryCaseInsensitive<object>()
                    {
                        {"Name", "Inserted name"}
                    },
                    Identifiers = new DictionaryCaseInsensitive<object>()
                    {
                        {"Id", 100}
                    }
                },
                new DbItem()
                {
                    Identifiers = new DictionaryCaseInsensitive<object>()
                    {
                        {"Id", 101}
                    },
                    Data =
                    {
                        {"Name", "Inserted name2"}
                    }
                }
            };

            try
            {
                var insertResult = await _sqlAdapter.InsertRows(tableName, data);
                Assert.True(insertResult.IsOk, insertResult.ToString());

                List<DbItem> items = new List<DbItem> {
                    new DbItem()
                    {
                        Identifiers = new DictionaryCaseInsensitive<object>()
                        {
                            {"Id", 100}
                        }
                    },
                    new DbItem()
                    {
                        Identifiers = new DictionaryCaseInsensitive<object>()
                        {
                            {"Id", 101}
                        }
                    }
                };

                var deleteResult = await _sqlAdapter.DeleteItems(tableName, items);
                Assert.True(deleteResult.OperationResponse.IsOk);
            }
            finally
            {
                // Cleanup: Ensure the inserted rows are deleted
                await _sqlAdapter.DeleteItems(tableName, data);
            }
        }
    }
}