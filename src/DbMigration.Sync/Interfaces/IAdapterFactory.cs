using DbMigration.Domain.Model;

namespace DbMigration.Sync.Interfaces
{
    public interface IAdapterFactory
    {
        /// <summary>
        /// Determines the adapter to create based on the configuration type. E.g. SqlAdapterConfig or InMemoryAdapterConfig
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configuration"></param>
        /// <returns></returns>
        Task<DbValueOperationResponse<IAdapter>> CreateAdapter<T>(T configuration); 

    }
}
