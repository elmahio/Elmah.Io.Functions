using Elmah.Io.Client;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Elmah.Io.Functions
{
    /// <summary>
    /// This filter logs a healthy or unhealthy heartbeat to elmah.io, depending if the functions runs with or without exceptions. Register using FunctionStartup.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    public class ElmahIoHeartbeatFilter : IFunctionInvocationFilter
#pragma warning restore CS0618 // Type or member is obsolete
    {
        private readonly ElmahIoFunctionOptions options;
        private ElmahioAPI api;
        internal static string _assemblyVersion = typeof(ElmahIoHeartbeatFilter).Assembly.GetName().Version.ToString();

        public ElmahIoHeartbeatFilter(IOptions<ElmahIoFunctionOptions> options)
        {
            this.options = options.Value;
            if (string.IsNullOrWhiteSpace(this.options.ApiKey)) throw new ArgumentNullException(nameof(this.options.ApiKey));
            if (this.options.LogId == null || this.options.LogId == Guid.Empty) throw new ArgumentNullException(nameof(this.options.LogId));
            if (string.IsNullOrWhiteSpace(this.options.HeartbeatId)) throw new ArgumentNullException(nameof(this.options.HeartbeatId));
        }

#pragma warning disable CS0618 // Type or member is obsolete
        public async Task OnExecutedAsync(FunctionExecutedContext executedContext, CancellationToken cancellationToken)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            if (api == null)
            {
                api = (ElmahioAPI)ElmahioAPI.Create(options.ApiKey);
                api.HttpClient.Timeout = options.Timeout;
                api.HttpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("Elmah.Io.Functions", _assemblyVersion)));
            }

            if (executedContext.FunctionResult.Succeeded)
            {
                await api.Heartbeats.HealthyAsync(options.LogId, options.HeartbeatId);
            }
            else
            {
                await api.Heartbeats.UnhealthyAsync(options.LogId, options.HeartbeatId, executedContext.FunctionResult.Exception?.ToString());

            }
        }

#pragma warning disable CS0618 // Type or member is obsolete
        public Task OnExecutingAsync(FunctionExecutingContext executingContext, CancellationToken cancellationToken)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            return Task.CompletedTask;
        }
    }
}
