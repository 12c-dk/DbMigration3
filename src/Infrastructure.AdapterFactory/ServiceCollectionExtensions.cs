using Microsoft.Extensions.DependencyInjection;
using Adapter.Sql;
using DbMigration.Sync.Interfaces;

namespace Infrastructure.AdapterFactory
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAdapterFactory(this IServiceCollection services)
        {
            services.AddTransient<IAdapterFactory, AdapterFactory>();
            services.AddSqlAdapter();

            return services;
        }
    }
}
