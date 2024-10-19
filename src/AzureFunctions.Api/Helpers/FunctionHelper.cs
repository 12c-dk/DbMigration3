using System;
using System.IO;
using System.Threading.Tasks;
using AzureFunctions.Api.Managers;
using DbMigration.Common.Legacy.ClientStorage.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureFunctions.Api.Helpers
{
    public class FunctionHelper
    {
        private readonly ILogger<FunctionHelper> _log;
        private readonly ConfigManager _configManager;

        public FunctionHelper(ILogger<FunctionHelper> log, ConfigManager configManager)
        {
            _log = log;
            _configManager = configManager;
        }

        public void ValidateConfigurations()
        {
            ValidateConfigValue(Constants.TenantId);

        }

        private void ValidateConfigValue(string settingName)
        {
            string value = _configManager.GetConfigValue(settingName);

            if (string.IsNullOrEmpty(value))
            {
                throw new Exception($"Error. The app setting {settingName} is unavailable. You need to update the app settings in the function app");
            }
        }

        public async Task<T> DeserializeBody<T>(string functionName, HttpRequest req) where T : class
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            _log.LogInformation($"Body received: {requestBody}");
            if (string.IsNullOrEmpty(requestBody))
            {
                //return _functionHelper.GetBadRequestResponse($"{this.GetType().Name}: Request body is null or empty");
                return null;
            }

            T outputObject = JsonConvert.DeserializeObject<T>(requestBody);
            return outputObject;
        }

        public BadRequestObjectResult GetBadRequestResponse(string outputMessage)
        {
            _log.LogInformation($"Output: {outputMessage}");
            return new BadRequestObjectResult(outputMessage);
        }

        public BadRequestObjectResult GetBadRequestResponse(string functionName, Exception ex)
        {
            string errorMessage = $"{functionName}: Unexpected error occurred: {ex.Message} ";
            _log.LogError(errorMessage + ex.StackTrace);
            return new BadRequestObjectResult(errorMessage);
        }

        public NotFoundObjectResult GetNotFoundResponse(string outputMessage)
        {
            _log.LogInformation($"Output: {outputMessage}");
            return new NotFoundObjectResult(outputMessage);
        }

        public OkObjectResult GetOkJsonResponse<T>(string functionName, T outputObject)
        {
            string jsonResponse = JsonConvert.SerializeObject(outputObject);
            _log.LogInformation($"{functionName}: Completed successfully with output: {jsonResponse}. ");
            return new OkObjectResult(outputObject);
        }

        public OkObjectResult GetOkResponse(string functionName, string outputString)
        {
            _log.LogInformation($"{functionName}: Completed successfully with output: {outputString}. ");
            return new OkObjectResult(outputString);
        }

        public OkObjectResult GetOkObjectResponse(string functionName, object outputObject)
        {
            string jsonResponse = JsonConvert.SerializeObject(outputObject);
            _log.LogInformation($"{functionName}: Completed successfully with output: {jsonResponse}. ");
            return new OkObjectResult(outputObject);
        }

    }
}
