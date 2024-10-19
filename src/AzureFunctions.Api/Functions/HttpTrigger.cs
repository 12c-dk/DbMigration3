using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using DbMigration.Common.Legacy.ClientStorage.Repositories;

namespace AzureFunctions.Api.Functions
{
    public class HttpTrigger
    {
        private readonly ConfigManager _configManager;

        public HttpTrigger(ConfigManager configManager)
        {
            _configManager = configManager;
        }

        [FunctionName("HttpTrigger")]
        public Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("HttpTrigger called");

            string serviceUsername = _configManager.GetConfigValue("ServiceUsername");

            log.LogInformation($"HttpTrigger completed successfully with Username {serviceUsername}");
            return Task.FromResult<IActionResult>(new OkObjectResult("Success"));

        }
    }
}
