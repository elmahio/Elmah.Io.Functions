using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Elmah.Io.Functions.TimerTriggerWithHeartbeat80
{
    public class Function1
    {
        // Use the following attribute to test missing heartbeat (after one unhealthy or healthy heartbeat)
        //[Disable]
        [FunctionName("Function1")]
        public void Run([TimerTrigger("0 */5 * * * *", RunOnStartup = true)]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            // Use the following to test unhealthy heartbeat
            //throw new Exception("Error in function");
        }
    }
}
