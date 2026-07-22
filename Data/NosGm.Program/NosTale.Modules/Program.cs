using log4net;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NosGm.Configuration.Helper;
using NosTale.Module.Bazaar;
using NosGm.Core;
using System.Threading.Tasks;

namespace NosTale.Modules
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            ConfigurationHelper.CustomisationRegistration();

            Logger.InitializeLogger(LogManager.GetLogger(typeof(Program)));
            var web = BuildHost(args);
            var bazaarManager = (BazaarManager)web.Services.GetService(typeof(BazaarManager));
            bazaarManager.Initialize();

            await web.RunAsync().ConfigureAwait(false);
        }

        public static IWebHost BuildHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((_, conf) =>
                {
                    conf.AddYamlFile("modules.yml", optional: false, reloadOnChange: true)
                        .AddEnvironmentVariables();
                })
                .UseStartup<Startup>()
                .Build();
    }
}
