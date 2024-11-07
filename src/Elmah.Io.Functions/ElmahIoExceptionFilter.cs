using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Elmah.Io.Functions
{
    /// <summary>
    /// This filter logs all uncaught exceptions happening during a function. Register using FunctionStartup.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    public class ElmahIoExceptionFilter : IFunctionExceptionFilter, IFunctionInvocationFilter
#pragma warning restore CS0618 // Type or member is obsolete
    {
        private readonly ElmahIoFunctionOptions options;

        /// <summary>
        /// Create a new instance of the ElmahIoExceptionFilter class. The constructor is intended for DI to use when setting up the filter.
        /// </summary>
        public ElmahIoExceptionFilter(IOptions<ElmahIoFunctionOptions> options)
        {
            this.options = options.Value;
            MessageShipper.CreateInstallation(this.options);
        }

        /// <summary>
        /// This method is called by Azure Functions when a function throws an uncaught exception. It is not intended to be called manually.
        /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
        public async Task OnExceptionAsync(FunctionExceptionContext exceptionContext, CancellationToken cancellationToken)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            HttpContext httpContext = null;

            foreach (var argument in exceptionContext.Properties.Keys.ToList())
            {
                switch (exceptionContext.Properties[argument])
                {
                    case HttpRequest request:
                        httpContext = request.HttpContext;
                        exceptionContext.Properties.Remove(argument);
                        break;
                }
            }

            await MessageShipper.Ship(exceptionContext, httpContext, options, cancellationToken);
        }

        /// <summary>
        /// This method is called by Azure Functions when a function has been executed. It is not intended to be called manually.
        /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
        public Task OnExecutedAsync(FunctionExecutedContext executedContext, CancellationToken cancellationToken)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            // Save context arguments like the HTTP context for HTTP triggered functions
            foreach (var arg in executedContext.Arguments)
            {
                executedContext.Properties.Add(arg.Key, arg.Value);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// This method is called by Azure Functions before a function is executed. It is not intended to be called manually.
        /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
        public Task OnExecutingAsync(FunctionExecutingContext executingContext, CancellationToken cancellationToken)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            return Task.CompletedTask;
        }
    }
}
