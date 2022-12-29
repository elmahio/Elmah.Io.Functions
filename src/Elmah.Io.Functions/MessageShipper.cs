using Elmah.Io.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Elmah.Io.Functions
{
    internal class MessageShipper
    {
        internal static string _assemblyVersion = typeof(MessageShipper).Assembly.GetName().Version.ToString();
#pragma warning disable CS0618 // Type or member is obsolete
        internal static string _functionsAssemblyVersion = typeof(FunctionExceptionContext).Assembly.GetName().Version.ToString();

        internal static IElmahioAPI elmahIoClient;

        public static async Task Ship(FunctionExceptionContext exceptionContext, HttpContext context, ElmahIoFunctionOptions options, CancellationToken cancellationToken = default)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            var exception = exceptionContext.Exception;
            var baseException = exception?.GetBaseException();
            var createMessage = new CreateMessage
            {
                DateTime = DateTime.UtcNow,
                Detail = Detail(exception),
                Type = baseException?.GetType().FullName,
                Title = baseException.Message,
                Data = Data(exceptionContext),
                Cookies = Cookies(context),
                Form = Form(context),
                Hostname = Hostname(context),
                ServerVariables = ServerVariables(context),
                StatusCode = StatusCode(exception, context),
                Url = context?.Request?.Path.Value,
                QueryString = QueryString(context),
                Method = context?.Request?.Method,
                Severity = Severity(exception, context),
                Source = Source(baseException),
                Application = options.Application,
            };

            if (options.OnFilter != null && options.OnFilter(createMessage))
            {
                return;
            }

            if (elmahIoClient == null)
            {
                elmahIoClient = ElmahioAPI.Create(options.ApiKey, new ElmahIoOptions
                {
                    Timeout = options.Timeout,
                    UserAgent = UserAgent(),
                });

                elmahIoClient.Messages.OnMessage += (sender, args) =>
                {
                    options.OnMessage?.Invoke(args.Message);
                };
                elmahIoClient.Messages.OnMessageFail += (sender, args) =>
                {
                    options.OnError?.Invoke(args.Message, args.Error);
                };
            }

            try
            {
                await elmahIoClient.Messages.CreateAndNotifyAsync(options.LogId, createMessage, cancellationToken);
            }
            catch (Exception e)
            {
                options.OnError?.Invoke(createMessage, e);
                // If there's a Exception while generating the error page, re-throw the original exception.
            }
        }

        /// <summary>
        /// Combine properties from exception Data dictionary and Azure Functions filter context properties
        /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
        private static IList<Item> Data(FunctionExceptionContext exceptionContext)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            var data = new List<Item>();
            var exceptionData = exceptionContext.Exception?.ToDataList();
            if (exceptionData?.Count > 0)
            {
                data.AddRange(exceptionData);
            }

            foreach (var property in exceptionContext.Properties.Where(p => !string.IsNullOrWhiteSpace(p.Key) && p.Key != Constants.StopwatchKeyName))
            {
                data.Add(new Item { Key = property.Key, Value = property.Value?.ToString() });
            }

            data.Add(new Item { Key = nameof(exceptionContext.FunctionInstanceId), Value = exceptionContext.FunctionInstanceId.ToString() });
            data.Add(new Item { Key = nameof(exceptionContext.FunctionName), Value = exceptionContext.FunctionName });

            return data;
        }

        private static string Hostname(HttpContext context)
        {
            var machineName = Environment.MachineName;
            if (!string.IsNullOrWhiteSpace(machineName)) return machineName;

            machineName = Environment.GetEnvironmentVariable("COMPUTERNAME");
            if (!string.IsNullOrWhiteSpace(machineName)) return machineName;

            return context?.Request?.Host.Host;
        }

        private static string Detail(Exception exception)
        {
            return exception?.ToString();
        }

        private static List<Item> Cookies(HttpContext httpContext)
        {
            return httpContext?
                .Request?
                .Cookies?
                .Keys
                .Select(k => new Item(k, httpContext.Request.Cookies[k])).ToList();
        }

        private static List<Item> Form(HttpContext httpContext)
        {
            try
            {
                return httpContext?
                    .Request?
                    .Form?
                    .Keys
                    .Select(k => new Item(k, httpContext.Request.Form[k])).ToList();
            }
            catch (InvalidOperationException)
            {
                // Request not a form POST or similar
            }

            return null;
        }

        private static List<Item> ServerVariables(HttpContext httpContext)
        {
            return httpContext?
                .Request?
                .Headers?
                .Keys
                .Select(k => new Item(k, httpContext.Request.Headers[k])).ToList();
        }

        private static List<Item> QueryString(HttpContext httpContext)
        {
            return httpContext?
                .Request?
                .Query?
                .Keys
                .Select(k => new Item(k, httpContext?.Request.Query[k])).ToList();
        }

        private static int? StatusCode(Exception exception, HttpContext context)
        {
            if (exception != null)
            {
                // If an exception is thrown, but the response has a successful status code,
                // it is because the exception filter is running before the correct
                // status code is assigned the response. Override it with 500.
                return context?.Response?.StatusCode < 400 ? 500 : context?.Response?.StatusCode;
            }

            return context?.Response?.StatusCode;
        }

        private static string Severity(Exception exception, HttpContext context)
        {
            var statusCode = StatusCode(exception, context);

            if (statusCode.HasValue && statusCode >= 400 && statusCode < 500) return Client.Severity.Warning.ToString();
            if (statusCode.HasValue && statusCode >= 500) return Client.Severity.Error.ToString();
            if (exception != null) return Client.Severity.Error.ToString();

            return null; // Let elmah.io decide when receiving the message
        }

        private static string Source(Exception baseException)
        {
            return baseException?.Source;
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
