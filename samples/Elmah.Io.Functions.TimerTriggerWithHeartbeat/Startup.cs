﻿using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

[assembly: FunctionsStartup(typeof(Elmah.Io.Functions.TimerTriggerWithHeartbeat.Startup))]

namespace Elmah.Io.Functions.TimerTriggerWithHeartbeat
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
                o.HeartbeatId = config["heartbeatId"];
            });

            builder.Services.AddSingleton<IFunctionFilter, ElmahIoHeartbeatFilter>();
        }
    }
}
