using Elmah.Io.Client;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Elmah.Io.Functions.Test
{
    internal class ElmahIoExceptionFilterTest
    {
        [Test]
        public async Task CanLogException()
        {
            // Arrange
            var options = Options.Create(new ElmahIoFunctionOptions { ApiKey = "API_KEY", LogId = Guid.NewGuid() });
            var filter = new ElmahIoExceptionFilter(options);

            var elmahIoClient = Substitute.For<IElmahioAPI>();
            var messagesClient = Substitute.For<IMessagesClient>();
            elmahIoClient.Messages.Returns(messagesClient);

            MessageShipper.elmahIoClient = elmahIoClient;
            var innerException = new FormatException("Inner");
            var outerException = new Exception("Outer", innerException);

            // Act
#pragma warning disable CS0618 // Type or member is obsolete
            await filter.OnExceptionAsync(new FunctionExceptionContext(
                Guid.Empty,
                "MyFunction",
                NullLogger.Instance,
                ExceptionDispatchInfo.Capture(outerException),
                new Dictionary<string, object>()), CancellationToken.None);
#pragma warning restore CS0618 // Type or member is obsolete

            // Assert
            await messagesClient
                .Received()
                .CreateAndNotifyAsync(
                    Arg.Is(options.Value.LogId),
                    Arg.Is<CreateMessage>(msg =>
                        msg.Title == "Inner"
                        && msg.DateTime.HasValue
                        && msg.Detail != null
                        && msg.Type == "System.FormatException"
                        && msg.Hostname != null
                        && msg.Severity == "Error"
                        && msg.Source == innerException.Source));
        }
    }
}
