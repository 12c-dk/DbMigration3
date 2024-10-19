using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using AzureFunctions.Api.Helpers;
using DbMigration.Common.Legacy.Model;
using DbMigration.Common.Legacy.ClientStorage.Repositories;

namespace AzureFunctions.Api.Functions
{
    public class GetProject
    {
        private readonly FunctionHelper _functionHelper;
        private readonly ProjectRepository _projectRepository;

        public GetProject(FunctionHelper functionHelper, ProjectRepository projectRepository)
        {
            _functionHelper = functionHelper;
            _projectRepository = projectRepository;
        }

        [FunctionName("GetProject")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            //[Table("sampleentity", Connection = "AzureWebJobsStorage")] CloudTable cloudTable,
            ILogger log)
        {
            log.LogInformation("GetProject called");

            ProjectGetRequest getRequest = await _functionHelper.DeserializeBody<ProjectGetRequest>("GetProject", req);

            Project proj1 = await _projectRepository.GetProject(getRequest.ProjectId);
            
            return _functionHelper.GetOkObjectResponse(GetType().Name, proj1);

        }
    }
}
