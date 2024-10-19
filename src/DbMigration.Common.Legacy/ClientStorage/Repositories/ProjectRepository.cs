//using AzureFunctions.Api.Managers;

using DbMigration.Common.Legacy.ClientStorage.Clients;
using DbMigration.Common.Legacy.Model;

namespace DbMigration.Common.Legacy.ClientStorage.Repositories
{
    public class ProjectRepository
    {
        private readonly TableStorageClient _projectTableClient;
        private readonly string _projectPartitionKey = "project";

        public ProjectRepository()
        {

        }

        public ProjectRepository(ConfigManager configManager)
        {
            string storageConnectionString = configManager.GetConfigValue("AzureWebJobsStorage");
            _projectTableClient = new StorageClient(storageConnectionString, configManager).GetTableStorageClient("Projects");

            SeedData().Wait();
        }

        private async Task SeedData()
        {
            if (await GetProject("A01") == null)
            {
                await CreateProject(new Project() { PartitionKey = _projectPartitionKey, RowKey = "A01", Title = "Sample title", Name = "Sampleproject" });
            }

        }

        public virtual async Task<Project> GetProject(string projectId)
        {
            Project proj1 = await _projectTableClient.Retrieve<Project>(_projectPartitionKey, projectId);
            return proj1;
        }

        public virtual async Task<Project> CreateProject(Project proj)
        {
            await _projectTableClient.InsertOrMerge(proj);
            Project project = await GetProject(proj.RowKey);
            return project;
        }

        public virtual async Task DeleteProject(string rowKey)
        {
            Project project = await GetProject(rowKey);

            if (project == null)
            {
                return;
            }

            await _projectTableClient.Delete(project);


        }

    }
}
