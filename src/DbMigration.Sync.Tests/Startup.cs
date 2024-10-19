using Microsoft.Extensions.DependencyInjection;
using DbMigration.Sync.Repositories;

namespace DbMigration.Sync.Tests
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            ConfigManager configManager = new ConfigManager();
            services.AddSingleton(configManager);

            services.BuildServiceProvider();
        }
    }
}
