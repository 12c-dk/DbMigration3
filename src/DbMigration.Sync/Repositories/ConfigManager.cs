using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace DbMigration.Sync.Repositories
{
    public class ConfigManager
    {
        private IConfigurationRoot Configurations { get; }

        public ConfigManager()
        {
            var realPath = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location)?.FullName;

            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json", true) //Path for settings when running unit tests
                .AddJsonFile("debug.settings.json", true) //Path for settings when running unit tests
                .AddJsonFile($"{realPath}\\..\\secrets.settings.json", true) //Path for secrets when running function app locally
                .AddJsonFile("secrets.settings.json", true) //Path for secrets when running unit tests
                .Build();

            Configurations = config;
        }

        // ReSharper disable once UnusedMember.Local
        private string GetBasePath()
        {
            using var processModule = Process.GetCurrentProcess().MainModule;
            return Path.GetDirectoryName(processModule?.FileName);
        }

        public string GetConfigValue(string key)
        {
            string output = Configurations["Values:" + key] ?? Configurations[key];
            return output;
        }

    }
}
