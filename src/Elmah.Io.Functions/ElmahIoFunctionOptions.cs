using Elmah.Io.Client.Models;
using System;

namespace Elmah.Io.Functions
{
    public class ElmahIoFunctionOptions
    {
        public string ApiKey { get; set; }

        public Guid LogId { get; set; }

        public string Application { get; set; }

        public Action<CreateMessage> OnMessage { get; set; }

        public Action<CreateMessage, Exception> OnError { get; set; }

        public Func<CreateMessage, bool> OnFilter { get; set; }    }
}
