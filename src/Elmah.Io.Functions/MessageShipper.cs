using Elmah.Io.Client;
using Elmah.Io.Client.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Elmah.Io.Functions
{
    internal class MessageShipper
    {
        internal static string _assemblyVersion = typeof(MessageShipper).Assembly.GetName().Version.ToString();

#pragma warning disable CS0618 // Type or member is obsolete
        public static async Task Ship(FunctionExceptionContext exceptionContext, HttpContext context, ElmahIoFunctionOptions options)
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
                Hostname = context?.Request?.Host.Host,
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

            var elmahioApi = (ElmahioAPI)ElmahioAPI.Create(options.ApiKey);
            elmahioApi.HttpClient.Timeout = options.Timeout;
            elmahioApi.HttpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("Elmah.Io.Functions", _assemblyVersion)));

            elmahioApi.Messages.OnMessage += (sender, args) =>
            {
                options.OnMessage?.Invoke(args.Message);
            };
            elmahioApi.Messages.OnMessageFail += (sender, args) =>
            {
                options.OnError?.Invoke(args.Message, args.Error);
            };

            try
            {
                await elmahioApi.Messages.CreateAndNotifyAsync(options.LogId, createMessage);
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

            foreach (var property in exceptionContext.Properties)
            {
                data.Add(new Item { Key = property.Key, Value = property.Value?.ToString() });
            }

            data.Add(new Item { Key = nameof(exceptionContext.FunctionInstanceId), Value = exceptionContext.FunctionInstanceId.ToString() });

            return data;
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
    }
}
