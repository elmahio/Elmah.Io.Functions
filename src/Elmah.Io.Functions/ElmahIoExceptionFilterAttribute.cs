using Elmah.Io.Client;
using Elmah.Io.Client.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
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
            if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentNullException(nameof(apiKey));
            if (string.IsNullOrWhiteSpace(logId)) throw new ArgumentNullException(nameof(logId));

            ApiKey = apiKey;
            LogId = logId;
        }

        public async override Task OnExceptionAsync(FunctionExceptionContext exceptionContext, CancellationToken cancellationToken)
        {
            if (exceptionContext?.Exception == null) return;

            var exception = exceptionContext.Exception;

            var resolver = new DefaultNameResolver();
            var apiKey = resolver.ResolveWholeString(ApiKey);

            if (!Guid.TryParse(resolver.ResolveWholeString(LogId), out Guid logId)) throw new ArgumentException("Log ID not a guid");

            var api = ElmahioAPI.Create(apiKey);
            await api.Messages.CreateAndNotifyAsync(logId, new CreateMessage
            {
                Title = exception.Message,
                DateTime = DateTime.UtcNow,
                Detail = exception.ToString(),
                Type = exception.GetType().Name,
                Data = Data(exceptionContext),
                Severity = Severity.Error.ToString(),
                Application = exceptionContext.FunctionName,
                Source = Source(exception),
            });
        }

        /// <summary>
        /// Combine properties from exception Data dictionary and Azure Functions filter context properties
        /// </summary>
        private IList<Item> Data(FunctionExceptionContext exceptionContext)
        {
            var data = new List<Item>();
            var exceptionData = exceptionContext.Exception.ToDataList();
            if (exceptionData != null)
            {
                data.AddRange(exceptionData);
            }

            foreach (var property in exceptionContext.Properties)
            {
                data.Add(new Item { Key = property.Key, Value = property.Value?.ToString() });
            }

            return data;
        }

        private string Source(Exception exception)
        {
            var ex = exception;
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
            }

            return ex?.Source;
        }
    }
}
