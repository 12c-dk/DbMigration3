using AzureFunctions.Api;
using AzureFunctions.Api.Helpers;
using DbMigration.Common.Legacy.ClientStorage.Clients;
using DbMigration.Common.Legacy.ClientStorage.Handlers;
using DbMigration.Common.Legacy.ClientStorage.Repositories;
using DbMigration.Common.Legacy.Handlers;
using DbMigration.Common.Legacy.Helpers;
using DbMigration.Common.Legacy.Messaging;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

//using ConfigManager = AzureFunctions.Api.Managers.ConfigManager;


[assembly: FunctionsStartup(typeof(Startup))]
namespace AzureFunctions.Api
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            ConfigManager configManager = new ConfigManager();

            var services = builder.Services;
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConsole();
            });

            services.AddSingleton(configManager);
            services.AddSingleton<FunctionHelper>();
            services.AddSingleton<ProjectRepository>();

            StorageClient storageClient = new StorageClient(configManager.GetConfigValue("AzureWebJobsStorage"), configManager);
            services.AddSingleton(storageClient);
            services.AddSingleton<DbConnectionsRepository>();

            DbTableCacheRepository dbTableRepository = new DbTableCacheRepository(storageClient);
            services.AddSingleton(dbTableRepository);

            services.AddSingleton<DbIndexCacheRepository>();

            //services.AddSingleton<TableStorageClient>();

            services.AddSingleton<MessageFilterHandler>();
            services.AddSingleton<CopyJobHandler>();
            services.AddSingleton<LoadSourceRowsHandler>();
            services.AddSingleton<TransformHandler>();
            services.AddSingleton<PushHandler>();
            services.AddSingleton<MessageHelper>();

            services.BuildServiceProvider();
            
        }
    }

}
