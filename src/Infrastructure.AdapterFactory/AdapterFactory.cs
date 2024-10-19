using Adapter.Sql;
using DbMigration.Domain.Model;
using DbMigration.Sync.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.AdapterFactory
{
    public class AdapterFactory : IAdapterFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public AdapterFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<DbValueOperationResponse<IAdapter>> CreateAdapter<T>(T configuration)
        {
            //Switch on configuration type if it is a InMemoryAdapterConfig or SqlAdapterConfig
            //Return the appropriate adapter
            IAdapter adapter = configuration switch
            {
                InMemoryAdapterConfig => _serviceProvider.GetService<InMemoryAdapter>(),
                SqlAdapterConfig => _serviceProvider.GetService<SqlAdapter>(),
                _ => throw new ArgumentException("Unknown adapter configuration type")
            };
            DbOperationResponse setConfigResult = await adapter.SetConfiguration(configuration);

            if (setConfigResult.GeneralStatus == DbOperationResponseSeverity.Info)
            {
                DbValueOperationResponse<IAdapter> valueOperationResponse = new DbValueOperationResponse<IAdapter>(adapter) { OperationResponse = setConfigResult};
                return valueOperationResponse;
            }
            else
            {
                DbValueOperationResponse<IAdapter> valueOperationResponse = new DbValueOperationResponse<IAdapter>() { OperationResponse = setConfigResult };
                return valueOperationResponse;
            }
            
        }
    }

}
