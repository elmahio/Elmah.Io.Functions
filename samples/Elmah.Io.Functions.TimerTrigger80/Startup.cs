using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

[assembly: FunctionsStartup(typeof(Elmah.Io.Functions.TimerTrigger80.Startup))]

namespace Elmah.Io.Functions.TimerTrigger80
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            builder.Services.Configure<ElmahIoFunctionOptions>(o =>
            {
                o.ApiKey = config["apiKey"];
                o.LogId = new Guid(config["logId"]);

                // Optional set application name on all errors
                o.Application = "Azure Functions v4 application";

                // Optional enrich all errors with one or more properties
                o.OnMessage = m =>
                {
                    m.Version = "8.0.0";
                };

                // Enrich installation when notifying elmah.io after launch:
                //o.OnInstallation = installation =>
                //{
                //    installation.Name = "Azure Functions v4 application";
                //    var logger = installation.Loggers.FirstOrDefault(l => l.Type == "Elmah.Io.Functions");
                //    logger?.Properties.Add(new Elmah.Io.Client.Item("Foo", "Bar"));
                //};
            });

#pragma warning disable CS0618 // Type or member is obsolete
            builder.Services.AddSingleton<IFunctionFilter, ElmahIoExceptionFilter>();
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
