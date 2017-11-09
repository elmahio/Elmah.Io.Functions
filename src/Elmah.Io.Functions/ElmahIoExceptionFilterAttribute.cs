using Elmah.Io.Client;
using Elmah.Io.Client.Models;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Elmah.Io.Functions
{
    public class ElmahIoExceptionFilterAttribute : FunctionExceptionFilterAttribute
    {
        public string ApiKey { get; set; }

        public string LogId { get; set; }

        public ElmahIoExceptionFilterAttribute(string apiKey, string logId)
        {
            ApiKey = apiKey;
            LogId = logId;
        }

        public async override Task OnExceptionAsync(FunctionExceptionContext exceptionContext, CancellationToken cancellationToken)
        {
            if (exceptionContext?.Exception == null) return;

            var exception = exceptionContext.Exception;

            var api = ElmahioAPI.Create(ApiKey);
            await api.Messages.CreateAndNotifyAsync(new Guid(LogId), new CreateMessage
            {
                Title = exception.Message,
                DateTime = DateTime.UtcNow,
                Detail = exception.ToString(),
                Type = exception.GetType().Name,
                Data = exception.ToDataList(),
                Severity = Severity.Error.ToString(),
                Source = exceptionContext.FunctionName,
            });
        }
    }
}
