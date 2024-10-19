using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DbMigration.Common.Legacy.ClientStorage.Repositories;
using DbMigration.Common.Legacy.Model;

namespace AzureFunctions.Api.Tests.Mocks
{
    public class ProjectRepositoryFake : ProjectRepository
    {
        private readonly TestDataModel _testDataModel;

        public ProjectRepositoryFake(TestDataModel testDataModel)
        {
            _testDataModel = testDataModel;
        }

        public override Task<Project> GetProject(string projectId)
        {
            List<Project> projects = _testDataModel.Projects.Where(p => p.RowKey == projectId).ToList();
            if (projects.Count == 1)
            {
                return Task.FromResult(projects.First());
            }

            return Task.FromResult<Project>(null);
        }

        public override Task<Project> CreateProject(Project proj)
        {
            _testDataModel.Projects.Add(proj);
            
            return Task.FromResult(proj);

        }

        public override async Task DeleteProject(string rowKey)
        {
            var project = await GetProject(rowKey);
            _testDataModel.Projects.Remove(project);

        }
    }
}
