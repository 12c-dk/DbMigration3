using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using DbMigration.Sync.Interfaces;
using DbMigration.Sync.Repositories;
using Xunit;
using Adapter.Sql;
using Infrastructure.AdapterFactory;
using DbMigration.Domain.Model;
using DbMigration.Domain.DictionaryModels;
using DbMigration.Sync.UseCaseBasicSync;

namespace DbMigration.Sync.Tests
{
    public class SyncIntTests
    {
        private readonly SynchronizationModule _synchronizationModule;
        private readonly ConfigManager _configManager;
        private readonly IAdapterFactory _adapterFactory;

        public SyncIntTests(ConfigManager configManager)
        {

            _configManager = configManager;

            // Set up the DI container
            ServiceCollection serviceCollection = new ServiceCollection();

            // Register synchronization module
            serviceCollection.AddTransient<SynchronizationModule>();

            // Not using IAdapter interface because AdapterFactory needs to create concrete instances
            serviceCollection.AddAdapterFactory();

            serviceCollection.AddLogging();


            // Build the service provider
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Resolve and use the synchronization module
            _synchronizationModule = serviceProvider.GetRequiredService<SynchronizationModule>();
            _adapterFactory = serviceProvider.GetRequiredService<IAdapterFactory>();
        }


        [Fact]
        public async Task SyncTest()
        {
            // Setup 
            var srcSqlConfig = new SqlAdapterConfig { ConnectionString = _configManager.GetConfigValue("SourceConnectionString") };
            var tgtSqlConfig = new SqlAdapterConfig { ConnectionString = _configManager.GetConfigValue("TargetConnectionString") };

            // Cleanup items before
            var srcAdapterResponse = await _adapterFactory.CreateAdapter(srcSqlConfig);
            Assert.True(srcAdapterResponse.OperationResponse.IsOk, srcAdapterResponse.OperationResponse.ToString());
            var srcAdapter = srcAdapterResponse.ResponseValue;


            var items = await srcAdapter.GetTableData("TargetTable", selectFields: new List<string>(){"Id"});
            items.ForEach(i => i.Identifiers = i.Data);
            items.ForEach(i => i.Data = new DictionaryCaseInsensitive<object>());
            await srcAdapter.DeleteItems("TargetTable", items);

            // Act 
            await _synchronizationModule.SetupConnections(srcSqlConfig, tgtSqlConfig);
            await _synchronizationModule.Synchronize();
            
        }

        [Fact]
        public async Task SyncUpdateExistingItemsTest()
        {
            //This test fails currently. Tests desired target scenario. 

            // Setup 
            var srcSqlConfig = new SqlAdapterConfig { ConnectionString = _configManager.GetConfigValue("SourceConnectionString") };
            var tgtSqlConfig = new SqlAdapterConfig { ConnectionString = _configManager.GetConfigValue("TargetConnectionString") };
            
            // Setup
            var srcAdapterResponse = await _adapterFactory.CreateAdapter( srcSqlConfig);
            Assert.True(srcAdapterResponse.OperationResponse.IsOk, srcAdapterResponse.OperationResponse.ToString());
            IAdapter srcAdapter = srcAdapterResponse.ResponseValue;
            
            var targetAdapterResponse = await _adapterFactory.CreateAdapter(srcSqlConfig);
            Assert.True(targetAdapterResponse.OperationResponse.IsOk, targetAdapterResponse.OperationResponse.ToString());
            IAdapter targetAdapter = targetAdapterResponse.ResponseValue;

            //Remove items from target
            var items = await targetAdapter.GetTableData("TargetTable", selectFields: new List<string>() { "Id" });
            items.ForEach(i => i.Identifiers = i.Data);
            items.ForEach(i => i.Data = new DictionaryCaseInsensitive<object>());
            await targetAdapter.DeleteItems("TargetTable", items);

            // add source item to target
            var sourceItems = await srcAdapter.GetTableData("SourceTable", selectFields: new List<string>() { "Id" });
            var firstItem = sourceItems.First();
            DictionaryCaseInsensitive<object> data = firstItem.Data;
            var genericRow = data.ToDictionaryCaseInsensitive();
            Assert.NotNull(genericRow);

            //Insert item to target
            await targetAdapter.InsertRows("TargetTable", new List<DbItem>
            {
                new DbItem(null, new DictionaryCaseInsensitive<object>
                {
                    { "Id", data["Id"] },
                    { "Name", "RandomName" }
                })
            });

            // Act 
            await _synchronizationModule.SetupConnections(srcSqlConfig, tgtSqlConfig);
            DbValueCollectionOperationResponse<List<DbItem>> syncResult = await _synchronizationModule.Synchronize();


            // Assert
            Assert.True(syncResult.IsOk);
            Assert.True(syncResult.ResponseValue.Count() == 3);


            List<DbItem> targetAfter = await targetAdapter.GetTableData("TargetTable");

            var filteredItems = targetAfter.Where(t => t.Data.ContainsKey("Id") && t.Data["Id"] != null && t.Data["Id"].Equals(1)).ToList();
            
            // Assert that the filtered items are as expected
            Assert.NotEmpty(filteredItems);
            Assert.All(filteredItems, item => Assert.Equal(1, item.Data["Id"]));

            var filteredItems2 = targetAfter.Where(t => t.Data.ContainsKey("Id") && t.Data["Id"] != null && t.Data["Id"].Equals(2)).ToList();
            Assert.NotEmpty(filteredItems2);
            Assert.All(filteredItems2, item => Assert.Equal(2, item.Data["Id"]));

        }

    }
}
