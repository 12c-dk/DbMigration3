using AzureFunctions.Api.Tests.Mocks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using AzureFunctions.Api.Helpers;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;
using DbMigration.Common.Legacy.ClientStorage.Repositories;

namespace AzureFunctions.Api.Tests
{
    public class FunctionsTests
    {
        private readonly ConfigManager _configManager;
        readonly MockLogger<FunctionsTests> _mockLogger;
        readonly FunctionHelper _functionHelper;
        private readonly ProjectRepository _projectRepository;

        public FunctionsTests(ITestOutputHelper output, ConfigManager configManager, FunctionHelper functionHelper, ProjectRepository projectRepository)
        {
            _mockLogger = new MockLogger<FunctionsTests>(output);
            _configManager = configManager;
            _functionHelper = functionHelper;
            _projectRepository = projectRepository;
        }

        [Fact]
        public void HttpTriggerTest()
        {
            _mockLogger.LogInformation("HttpTriggerTest started");

            var query = new Dictionary<String, StringValues>();
            var header = new Dictionary<String, StringValues>();
            string body = "";

            var req = HttpMock.HttpRequestSetup(header, query, body);

            var httpTriggerFunc = new Functions.HttpTrigger(_configManager);

            var result = httpTriggerFunc.Run(req, _mockLogger).Result;

            OkObjectResult res = (OkObjectResult)result;

            string outputStr = res.Value.ToString();
            Assert.False(string.IsNullOrEmpty(res.Value.ToString()));
            Assert.NotNull(outputStr);
            Assert.Contains("Success", outputStr);


        }

        [Fact]
        public void GetProjectTest()
        {
            _mockLogger.LogInformation("GetProjectTest started");

            var query = new Dictionary<String, StringValues>();
            var header = new Dictionary<String, StringValues>();
            string body = File.ReadAllText("Samples/ProjectGetRequest.json");

            var req = HttpMock.HttpRequestSetup(header, query, body);

            var func = new Functions.GetProject(_functionHelper, _projectRepository);

            var result = func.Run(req, _mockLogger).Result;

            OkObjectResult res = (OkObjectResult)result;

            string outputStr = JsonConvert.SerializeObject(res.Value);
            Assert.False(string.IsNullOrEmpty(outputStr));
            Assert.Contains("Skagens perle", outputStr);

        }

    }
}
