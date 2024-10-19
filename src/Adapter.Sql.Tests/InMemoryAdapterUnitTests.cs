using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DbMigration.Domain.DictionaryModels;
using DbMigration.Domain.Model;
using DbMigration.Sync.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Adapter.Sql.Tests
{
    public class InMemoryAdapterUnitTests
    {
        //private readonly SynchronizationModule _synchronizationModule;
        private readonly InMemoryAdapter _inMemoryAdapter;
        private readonly ITestOutputHelper _iTestOutputHelper;

        public InMemoryAdapterUnitTests(ITestOutputHelper iTestOutputHelper)
        {
            _iTestOutputHelper = iTestOutputHelper;
                ConfigManager configManager = new ConfigManager();

            // Set up the DI container
            ServiceCollection serviceCollection = new ServiceCollection();

            serviceCollection.AddTransient<InMemoryAdapter>();

            serviceCollection.AddLogging();

            // Build the service provider
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Resolve and use the synchronization module
            _inMemoryAdapter = serviceProvider.GetRequiredService<InMemoryAdapter>();
        }

        [Fact]
        public async Task InsertTest()
        {
            string tableName = "TargetTable";
            var srcConfig = new InMemoryAdapterConfig { };
            _inMemoryAdapter?.SetConfiguration(srcConfig);

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

            Debug.Assert(_inMemoryAdapter != null, nameof(_inMemoryAdapter) + " != null");
            var result = await _inMemoryAdapter.InsertRows(tableName, data);
            Assert.NotNull(result);

            var matches = await _inMemoryAdapter.GetTableData(tableName, queryString: "Name = 'Inserted name'");
            Assert.True(matches.Count > 0);

            await _inMemoryAdapter.DeleteItems(tableName, data);
            
        }

        [Fact]
        public async Task UpdateTest()
        {
            string tableName = "TargetTable";
            var srcConfig = new InMemoryAdapterConfig { };
            await _inMemoryAdapter.SetConfiguration(srcConfig);

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
                },
                new DbItem
                {
                    Identifiers = new DictionaryCaseInsensitive<object>
                    {
                        {"Id", 110}
                    },
                    Data =
                    {
                        {"Name", "Inserted name 110"}
                    }
                }
            };

            try
            {
                var insertResult = await _inMemoryAdapter.InsertRows(tableName, data);
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

                var updateResult = await _inMemoryAdapter.UpdateRows(tableName, items);
                Assert.NotNull(updateResult);
                Assert.True(updateResult.OperationResult == OperationResultEnum.Success, "Update operation failed");

                var matches = await _inMemoryAdapter.GetTableData(tableName, queryString: "Id = 120");
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
                await _inMemoryAdapter.DeleteItems(tableName, deleteItems);
            }
        }

        [Fact]
        public async Task DeleteTest()
        {
            string tableName = "TargetTable";
            var srcConfig = new InMemoryAdapterConfig() { };
            await _inMemoryAdapter.SetConfiguration(srcConfig);

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
                var insertResult = await _inMemoryAdapter.InsertRows(tableName, data);
                Assert.True(insertResult.OperationResult != OperationResultEnum.Failure);

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

                var deleteResult = await _inMemoryAdapter.DeleteItems(tableName, items);
                Assert.True(deleteResult.OperationResult == OperationResultEnum.Success);
            }
            finally
            {
                // Cleanup: Ensure the inserted rows are deleted
                await _inMemoryAdapter.DeleteItems(tableName, data);
            }
        }


        [Fact]
        public async Task UpsertTest()
        {
            string tableName = "TargetTable";
            var srcConfig = new InMemoryAdapterConfig { };
            await _inMemoryAdapter.SetConfiguration(srcConfig);

            List<DbItem> data = new List<DbItem>
            {
                new DbItem
                {
                    Identifiers = new DictionaryCaseInsensitive<object>
                    {
                        {"Id", 130}
                    },
                    Data =
                    {
                        {"Name", "Inserted name 130"}
                    }
                },
                new DbItem
                {
                    Identifiers = new DictionaryCaseInsensitive<object>
                    {
                        {"Id", 140}
                    },
                    Data =
                    {
                        {"Name", "Inserted name 140"}
                    }
                }
            };

            try
            {
                var upsertResult = await _inMemoryAdapter.UpsertRows(tableName, data);
                Assert.NotNull(upsertResult);
                Assert.True(upsertResult.OperationResult == OperationResultEnum.Success, "Upsert operation failed");

                List<DbItem> matches = await _inMemoryAdapter.GetTableData(tableName);
                matches = matches.Where(m => (int) m.Identifiers["Id"] == 130).ToList();
                Assert.True(matches.Count > 0);
                Assert.Equal("Inserted name 130", matches[0].Data["Name"]);

                // Update the same row
                data[0].Data["Name"] = "Updated name 130";
                upsertResult = await _inMemoryAdapter.UpsertRows(tableName, data);
                Assert.NotNull(upsertResult);
                Assert.True(upsertResult.OperationResult == OperationResultEnum.Success, "Upsert operation failed");


                matches = await _inMemoryAdapter.GetTableData(tableName);
                matches = matches.Where(m => (int)m.Identifiers["Id"] == 140).ToList();
                Assert.True(matches.Count == 1);
                Assert.Equal("Inserted name 140", matches[0].Data["Name"]);

            }
            finally
            {
                // Cleanup: Delete the upserted row
                await _inMemoryAdapter.DeleteItems(tableName, data);
            }
        }


    }
}
