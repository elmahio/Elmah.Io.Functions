using Elmah.Io.Client;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
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
        private static readonly string _assemblyVersion = typeof(ElmahIoHeartbeatFilter).Assembly.GetName().Version.ToString();
#pragma warning disable CS0618 // Type or member is obsolete
        private static readonly string _functionsAssemblyVersion = typeof(IFunctionInvocationFilter).Assembly.GetName().Version.ToString();
#pragma warning restore CS0618 // Type or member is obsolete

        /// <summary>
        /// Create a new instance of the ElmahIoHeartbeatFilter class. The constructor is intended for DI to use when setting up the filter.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3928:Parameter names used into ArgumentException constructors should match an existing one ", Justification = "The arguments are on options")]
        public ElmahIoHeartbeatFilter(IOptions<ElmahIoFunctionOptions> options)
        {
            this.options = options.Value;
            if (string.IsNullOrWhiteSpace(this.options.ApiKey)) throw new ArgumentNullException(nameof(this.options.ApiKey));
            if (this.options.LogId == Guid.Empty) throw new ArgumentNullException(nameof(this.options.LogId));
            if (string.IsNullOrWhiteSpace(this.options.HeartbeatId)) throw new ArgumentNullException(nameof(this.options.HeartbeatId));
        }

        /// <summary>
        /// This method is called by Azure Functions when a function has been executed. It is not intended to be called manually.
        /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
        public async Task OnExecutedAsync(FunctionExecutedContext executedContext, CancellationToken cancellationToken)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            long? took = null;
            if (executedContext.Properties.ContainsKey(Constants.StopwatchKeyName) && executedContext.Properties[Constants.StopwatchKeyName] is Stopwatch stopwatch)
            {
                stopwatch.Stop();
                took = stopwatch.ElapsedMilliseconds;
            }

            if (api == null)
            {
                api = (ElmahioAPI)ElmahioAPI.Create(options.ApiKey, new ElmahIoOptions
                {
                    Timeout = options.Timeout,
                    UserAgent = UserAgent(),
                });
            }

            if (executedContext.FunctionResult.Succeeded)
            {
                await api.Heartbeats.HealthyAsync(options.LogId, options.HeartbeatId, took: took, cancellationToken: cancellationToken);
            }
            else
            {
                await api.Heartbeats.UnhealthyAsync(options.LogId, options.HeartbeatId, executedContext.FunctionResult.Exception?.ToString(), took: took, cancellationToken: cancellationToken);

            }
        }

        /// <summary>
        /// This method is called by Azure Functions before a function is executed. It is not intended to be called manually.
        /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
        public Task OnExecutingAsync(FunctionExecutingContext executingContext, CancellationToken cancellationToken)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            executingContext.Properties.Add(Constants.StopwatchKeyName, stopwatch);
            return Task.CompletedTask;
        }

        private static string UserAgent()
        {
            return new StringBuilder()
                .Append(new ProductInfoHeaderValue(new ProductHeaderValue("Elmah.Io.Functions", _assemblyVersion)).ToString())
                .Append(" ")
                .Append(new ProductInfoHeaderValue(new ProductHeaderValue("Microsoft.Azure.WebJobs", _functionsAssemblyVersion)).ToString())
                .ToString();
        }
    }
}
