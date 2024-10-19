using System;
using System.Threading.Tasks;
using DbMigration.Common.Legacy.Messaging;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Api.Functions
{
    public class TransformQueue
    {
        private readonly MessageFilterHandler _messageFilterHandler;

        public TransformQueue(MessageFilterHandler messageFilterHandler)
        {
            _messageFilterHandler = messageFilterHandler;
        }

        [FunctionName("TransformQueue")]
        public async Task Run([QueueTrigger("transform", Connection = "AzureWebJobsStorage" )]string myQueueItem, ILogger log)
        {
            try
            {
                log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");

                await _messageFilterHandler.HandleMessage(myQueueItem);

            }
            catch (Exception ex)
            {
                log.LogError(ex, "Unexpected exception occurred in TransformQueue");
                Console.WriteLine(ex);
                throw;
            }

        }
        
    }
}
