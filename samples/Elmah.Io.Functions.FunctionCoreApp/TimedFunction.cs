using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace Elmah.Io.Functions.FunctionCoreApp
{
    [ElmahIoExceptionFilter("API_KEY", "LOG_ID")]
    public static class TimedFunction
    {
        [FunctionName("TimedFunction")]
        public static void Run([TimerTrigger("0 */1 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

            throw new ApplicationException("Error from function");
        }
    }
}
