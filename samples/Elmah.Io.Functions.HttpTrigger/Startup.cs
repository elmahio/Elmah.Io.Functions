using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

[assembly: FunctionsStartup(typeof(Elmah.Io.Functions.HttpTrigger.Startup))]

namespace Elmah.Io.Functions.HttpTrigger
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
                o.OnMessage = m =>
                {
                    m.Version = "1.0.0";
                };
            });

#pragma warning disable CS0618 // Type or member is obsolete
            builder.Services.AddSingleton<IFunctionFilter, ElmahIoExceptionFilter>();
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
