using Microsoft.Extensions.DependencyInjection;

namespace Adapter.Sql
{
    // In Infrastructure.SqlAdapter Project
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSqlAdapter(this IServiceCollection services)
        {
            services.AddTransient<SqlAdapter>();
            return services;
        }
    }

}
