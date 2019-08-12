using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Elmah.Io.Functions
{
    public class ElmahIoExceptionFilter : IFunctionExceptionFilter, IFunctionInvocationFilter
    {
        private readonly ElmahIoFunctionOptions options;

        public ElmahIoExceptionFilter(IOptions<ElmahIoFunctionOptions> options)
        {
            this.options = options.Value;
        }

        public async Task OnExceptionAsync(FunctionExceptionContext exceptionContext, CancellationToken cancellationToken)
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
                    default:
                        break;
                }
            }

            await MessageShipper.Ship(exceptionContext, httpContext, options);
        }

        public async Task OnExecutedAsync(FunctionExecutedContext executedContext, CancellationToken cancellationToken)
        {
            // Save context arguments like the HTTP context for HTTP triggered functions
            foreach (var arg in executedContext.Arguments)
            {
                executedContext.Properties.Add(arg.Key, arg.Value);
            }
        }

        public async Task OnExecutingAsync(FunctionExecutingContext executingContext, CancellationToken cancellationToken)
        {
        }
    }
}
