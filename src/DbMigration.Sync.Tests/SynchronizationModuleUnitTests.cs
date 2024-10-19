using System.Collections.Generic;
using System.Threading.Tasks;
using DbMigration.Domain.DictionaryModels;
using DbMigration.Domain.Model;
using DbMigration.Sync.Interfaces;
using DbMigration.Sync.UseCaseBasicSync;
using Moq;
using Xunit;

namespace DbMigration.Sync.Tests
{
    public class SynchronizationModuleUnitTests
    {
        [Fact]
        public async Task Synchronize_ShouldFetchDataFromSourceAndSaveToTarget()
        {
            //Test that expected methods are called

            // Arrange
            var mockSourceAdapter = new Mock<IAdapter>();
            var mockTargetAdapter = new Mock<IAdapter>();
            var mockAdapterFactory = new Mock<IAdapterFactory>();

            // Sample data for FetchData and SaveData using Dictionary<string, object>
            var sampleData = new List<DictionaryCaseInsensitive<object>>
            {
                new DictionaryCaseInsensitive<object> { { "Key1", "Value1" } },
                new DictionaryCaseInsensitive<object> { { "Key2", "Value2" } }
            };

            // Sample data for GetTableData using DbItem
            var dbItems = new List<DbItem>
            {
                new DbItem { /* Initialize DbItem properties */ },
                new DbItem { /* Initialize DbItem properties */ }
            };

            // Setup for GetTableData (explicitly passing `null` for optional parameters)
            mockSourceAdapter.Setup(adapter => adapter.GetTableData(It.IsAny<string>(), null, null, null))
                .ReturnsAsync(dbItems);

            mockTargetAdapter.Setup(adapter => adapter.GetTableData(It.IsAny<string>(), null, null, null))
                .ReturnsAsync(dbItems);

            DbValueCollectionOperationResponse<List<DbItem>> upsertResponse = new DbValueCollectionOperationResponse<List<DbItem>>();
            mockTargetAdapter.Setup(adapter => adapter.UpsertRows(It.IsAny<string>(), It.IsAny<List<DbItem>>()))
                .ReturnsAsync(upsertResponse);
                    
            mockAdapterFactory.Setup(factory => factory.CreateAdapter(It.IsAny<object>()))
                .ReturnsAsync((object config) =>
                {
                    if (config is SourceConfig)
                    {
                        return new DbValueOperationResponse<IAdapter>(mockSourceAdapter.Object) ;
                    }
                    else if (config is TargetConfig)
                    {
                        return new DbValueOperationResponse<IAdapter>(mockTargetAdapter.Object);
                    }
                    return null;
                });

            var synchronizationModule = new SynchronizationModule(mockAdapterFactory.Object);

            // Act
            await synchronizationModule.SetupConnections(new SourceConfig(), new TargetConfig()); // Pass appropriate configurations
            await synchronizationModule.Synchronize();

            // Assert
            mockAdapterFactory.Verify(factory => factory.CreateAdapter(It.IsAny<SourceConfig>()), Times.Once);
            mockAdapterFactory.Verify(factory => factory.CreateAdapter(It.IsAny<TargetConfig>()), Times.Once);
            mockSourceAdapter.Verify(adapter => adapter.GetTableData(It.IsAny<string>(),null,null,null), Times.Once);
            mockTargetAdapter.Verify(adapter => adapter.UpsertRows(It.IsAny<string>(),It.IsAny<List<DbItem>>()), Times.Once);
        }
    }

    // Dummy configuration classes for demonstration purposes
    public class SourceConfig { }
    public class TargetConfig { }
}
