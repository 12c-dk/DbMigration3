using Microsoft.Extensions.DependencyInjection;
using AzureFunctions.Api.Helpers;
using AzureFunctions.Api.Tests.Mocks;
using DbMigration.Common.Legacy.ClientStorage.Repositories;

namespace AzureFunctions.Api.Tests
{
    class Startup
    {
        public delegate void LogRetryMessage(string messsage);

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();

            services.AddSingleton<ConfigManager>();
            services.AddSingleton<FunctionHelper>();
            //services.AddSingleton<ProjectRepository>();
            services.AddSingleton<ProjectRepository, ProjectRepositoryFake>();
            services.AddSingleton<TestDataModel>();

            services.BuildServiceProvider();
            
        }

        
    }
}
