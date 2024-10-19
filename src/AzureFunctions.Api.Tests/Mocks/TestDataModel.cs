using System.Collections.Generic;
using System.IO;
using DbMigration.Common.Legacy.Model;
using Newtonsoft.Json;

namespace AzureFunctions.Api.Tests.Mocks
{
    public class TestDataModel
    {
        public readonly List<Project> Projects;

        public TestDataModel()
        {
            string projects = File.ReadAllText("Samples\\Projects.json");
            Projects = JsonConvert.DeserializeObject<List<Project>>(projects);
        }

    }
}
