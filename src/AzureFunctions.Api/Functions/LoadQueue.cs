using System;
using System.Threading.Tasks;
using DbMigration.Common.Legacy.Messaging;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Api.Functions
{
    public class LoadQueue
    {
        private readonly MessageFilterHandler _messageFilterHandler;
        
        public LoadQueue(MessageFilterHandler messageFilterHandler)
        {
            _messageFilterHandler = messageFilterHandler;
        }

        [FunctionName("LoadQueue")]
        public async Task Run([QueueTrigger("load", Connection = "AzureWebJobsStorage")]string myQueueItem, ILogger log)
        {
            try
            {
                log.LogInformation($"C# Queue trigger function processing: {myQueueItem}");
                await _messageFilterHandler.HandleMessage(myQueueItem);

            }
            catch (Exception ex)
            {
                log.LogError(ex, "Unexpected exception occurred in LoadQueue");
                throw;
            }

        }
    }
}
