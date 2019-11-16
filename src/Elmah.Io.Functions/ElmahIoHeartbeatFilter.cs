using Elmah.Io.Client;
using Elmah.Io.Client.Models;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Elmah.Io.Functions
{
    public class ElmahIoHeartbeatFilter : IFunctionInvocationFilter
    {
        private readonly ElmahIoFunctionOptions options;
        private ElmahioAPI api;

        public ElmahIoHeartbeatFilter(IOptions<ElmahIoFunctionOptions> options)
        {
            this.options = options.Value;
            if (string.IsNullOrWhiteSpace(this.options.ApiKey)) throw new ArgumentNullException(nameof(this.options.ApiKey));
            if (this.options.LogId == null || this.options.LogId == Guid.Empty) throw new ArgumentNullException(nameof(this.options.LogId));
            if (string.IsNullOrWhiteSpace(this.options.HeartbeatId)) throw new ArgumentNullException(nameof(this.options.HeartbeatId));
        }

        public async Task OnExecutedAsync(FunctionExecutedContext executedContext, CancellationToken cancellationToken)
        {
            if (executedContext.FunctionResult.Succeeded)
            {
                await CreateAsync("Healthy");
            }
            else
            {
                await CreateAsync("Unhealthy", executedContext.FunctionResult.Exception?.ToString());

            }
        }

        private async Task CreateAsync(string result, string reason = null)
        {
            if (api == null)
            {
                api = new ElmahioAPI(new ApiKeyCredentials(options.ApiKey), HttpClientHandlerFactory.GetHttpClientHandler(new ElmahIoOptions()));
            }

            await api.Heartbeats.CreateAsync(options.HeartbeatId, options.LogId.ToString(), new CreateHeartbeat
            {
                Result = result,
                Reason = reason,
            });

        }
        public async Task OnExecutingAsync(FunctionExecutingContext executingContext, CancellationToken cancellationToken)
        {
        }
    }
}
