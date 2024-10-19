using Microsoft.Extensions.DependencyInjection;
using DbMigration.Common.Legacy.Messaging;
using DbMigration.Common.Legacy.Helpers;
using DbMigration.Common.Legacy.Handlers;
using DbMigration.Common.Legacy.ClientStorage.Repositories;
using DbMigration.Common.Legacy.ClientStorage.Handlers;
using DbMigration.Common.Legacy.ClientStorage.Clients;

namespace AzureFunctions.Api.IntegrationTest
{
    class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();

            ConfigManager configManager = new ConfigManager();
            services.AddSingleton(configManager);
            
            services.AddSingleton<ProjectRepository>();

            StorageClient storageClient = new StorageClient(configManager.GetConfigValue("AzureWebJobsStorage"), configManager);
            services.AddSingleton(storageClient);
            services.AddSingleton<DbConnectionsRepository>();

            DbTableCacheRepository dbTableRepository = new DbTableCacheRepository(storageClient);
            services.AddSingleton(dbTableRepository);

            services.AddSingleton<DbIndexCacheRepository>();

            services.AddSingleton<TableStorageClient>();

            services.AddSingleton<CopyJobHandler>();
            services.AddSingleton<MessageFilterHandler>();
            services.AddSingleton<LoadSourceRowsHandler>();
            services.AddSingleton<TransformHandler>();
            services.AddSingleton<PushHandler>();
            services.AddSingleton<MessageHelper>();


            services.BuildServiceProvider();

        }


    }
}
