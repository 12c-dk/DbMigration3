using Microsoft.Azure.WebJobs;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AzureFunctions.Api.Tests.Mocks
{
    /// <summary>
    /// Can be used with an Azure queue output binding. 
    /// I.e.: [Queue("userupdates")] IAsyncCollector&lt;string&gt; outputSbQueue
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AsyncCollectorMock<T> : IAsyncCollector<T>
    {
        // ReSharper disable once CollectionNeverQueried.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public List<T> Items { get; } = new List<T>();

        public Task AddAsync(T item, CancellationToken cancellationToken = default)
        {

            Items.Add(item);

            return Task.FromResult(true);
        }

        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }
}