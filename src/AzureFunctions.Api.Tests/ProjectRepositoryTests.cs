using System;
using System.Threading.Tasks;
using AzureFunctions.Api.Tests.Mocks;
using DbMigration.Common.Legacy.ClientStorage.Repositories;
using DbMigration.Common.Legacy.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace AzureFunctions.Api.Tests
{
    public class ProjectRepositoryTests
    {
        private readonly ProjectRepository _projectFake;
        private readonly TestDataModel _testDataModel;
        private readonly MockLogger<ProjectRepositoryTests> _log;

        public ProjectRepositoryTests(ProjectRepository projectFake, TestDataModel testDataModel, ITestOutputHelper output)
        {
            _projectFake = projectFake;
            _testDataModel = testDataModel;
            //_log = log;
            _log = new MockLogger<ProjectRepositoryTests>(output);
        }

        [Fact(Skip="Integration test")]
        public async Task CompareTest()
        {
            ConfigManager cfgMgr = new ConfigManager();
            ProjectRepository projRepo = new ProjectRepository(cfgMgr);
            Project proj = await projRepo.GetProject("A01");
            proj.Timestamp = DateTime.MaxValue;
            proj.ETag = null;
            string projString = JsonConvert.SerializeObject(proj);

            Project projFake = await _projectFake.GetProject("A01");
            projFake.Timestamp = DateTime.MaxValue;
            projFake.ETag = null;
            string projFakeString = JsonConvert.SerializeObject(projFake);
            
            Assert.Equal(projString, projFakeString);
        }

        [Fact(Skip="Integration test")]
        public async Task AlignProjectsTable()
        {
            ConfigManager cfgMgr = new ConfigManager();
            ProjectRepository projRepo = new ProjectRepository(cfgMgr);

            foreach (var project in _testDataModel.Projects)
            {
                await projRepo.DeleteProject(project.RowKey);

                await projRepo.CreateProject(project);
            }
        }

        [Fact]
        public async Task DeleteProject()
        {
            await _projectFake.DeleteProject("A02");

            _log.LogInformation($"Project count after delete: {_testDataModel.Projects.Count}");
            
            Assert.Single(_testDataModel.Projects);
        }
        
    }
}
