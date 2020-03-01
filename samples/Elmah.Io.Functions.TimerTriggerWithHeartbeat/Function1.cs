using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Elmah.Io.Functions.TimerTriggerWithHeartbeat
{
    public static class Function1
    {
        // Use the following attribute to test missing heartbeat (after one unhealthy or healthy heartbeat)
        //[Disable]
        [FunctionName("Function1")]
        public static void Run([TimerTrigger("0 */5 * * * *", RunOnStartup = true)]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            
            // Use the following to test unhealthy heartbeat
            //throw new Exception("Error in function");
        }
    }
}
