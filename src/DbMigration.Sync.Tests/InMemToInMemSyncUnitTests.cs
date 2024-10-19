using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Adapter.Sql;
using Castle.Components.DictionaryAdapter;
using DbMigration.Domain.DictionaryModels;
using DbMigration.Domain.Model;
using DbMigration.Sync.Interfaces;
using DbMigration.Sync.UseCaseBasicSync;
using Infrastructure.AdapterFactory;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace DbMigration.Sync.Tests
{
    public class InMemToInMemSyncUnitTests
    {
        private string _testTableName = "TestTable";

        public InMemToInMemSyncUnitTests()
        {
            var services = new ServiceCollection();
            services.AddTransient<InMemoryAdapter>();
            var provider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task SyncTest()
        {
            InMemoryAdapterConfig config1 = new InMemoryAdapterConfig();
            InMemoryAdapterConfig config2 = new InMemoryAdapterConfig();

            var mockSourceAdapter = new InMemoryAdapter();
            var mockTargetAdapter = new InMemoryAdapter(); 
            var mockAdapterFactory = new Mock<IAdapterFactory>();

            mockAdapterFactory.Setup(f => f.CreateAdapter(config1)).ReturnsAsync(new DbValueOperationResponse<IAdapter>(mockSourceAdapter));
            mockAdapterFactory.Setup(f => f.CreateAdapter(config2)).ReturnsAsync(new DbValueOperationResponse<IAdapter>(mockTargetAdapter));

            List<DbItem> sourceItems = new List<DbItem>()
            {
                new DbItem(new DictionaryCaseInsensitive<object>()
                    {
                        {"Id", 1}
                    },
                    new DictionaryCaseInsensitive<object>()
                    {
                        {"Name", "John"}
                    }
                ),
                new DbItem(new DictionaryCaseInsensitive<object>()
                    {
                        {"Id", 2}
                    },
                    new DictionaryCaseInsensitive<object>()
                    {
                        {"Name", "Jane Source"}
                    })

            };
            await mockSourceAdapter.InsertRows(_testTableName, sourceItems);

            List<DbItem> targetItems = new List<DbItem>()
            {
                new DbItem(new DictionaryCaseInsensitive<object>()
                    {
                        {"Id", 2}
                    },
                    new DictionaryCaseInsensitive<object>()
                    {
                        {"Name", "Jane Target"}
                    }),

                new DbItem(new DictionaryCaseInsensitive<object>()
                    {
                        {"Id", 3}
                    },
                    new DictionaryCaseInsensitive<object>()
                    {
                        {"Name", "Doe"}
                    }
                )

            };
            await mockTargetAdapter.InsertRows(_testTableName, targetItems);

            var synchronizationModule = new SynchronizationModule(mockAdapterFactory.Object);

            // Act
            DbOperationResponse setupConnectionsResponse = await synchronizationModule.SetupConnections(config1, config2);
            Assert.True(setupConnectionsResponse.IsOk);
            DbValueCollectionOperationResponse<List<DbItem>> result = await synchronizationModule.Synchronize();


            //Assert
            List<DbItem> targetItemsAfter = await mockTargetAdapter.GetTableData(_testTableName);
            Assert.True(targetItemsAfter.Count == 3);
            Assert.True(
                (string)targetItemsAfter.First(i => ((int)i.Identifiers["Id"] == 1)).Data["Name"] == "John"
            );
            Assert.True(
                (string)targetItemsAfter.First(i => ((int)i.Identifiers["Id"] == 2)).Data["Name"] == "Jane Source"
            );
            Assert.True(
                (string)targetItemsAfter.First(i => ((int)i.Identifiers["Id"] == 3)).Data["Name"] == "Doe"
            );

            var responseValueOutputItem2 = result.ResponseValue.First(r => (int)r.Identifiers["Id"] == 2);
            Assert.True((string)responseValueOutputItem2.Data["Name"] == "Jane Source");
            Assert.True(result.OperationResponse.GeneralStatus == DbOperationResponseSeverity.Info);
            Assert.True(result.OperationResponse.ItemStatus == DbOperationResponseSeverity.Info);

        }
    }
}
