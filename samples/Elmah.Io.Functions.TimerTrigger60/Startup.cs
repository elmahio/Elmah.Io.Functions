using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

[assembly: FunctionsStartup(typeof(Elmah.Io.Functions.TimerTrigger60.Startup))]

namespace Elmah.Io.Functions.TimerTrigger60
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
                    m.Version = "6.0.0";
                };
            });

#pragma warning disable CS0618 // Type or member is obsolete
            builder.Services.AddSingleton<IFunctionFilter, ElmahIoExceptionFilter>();
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
