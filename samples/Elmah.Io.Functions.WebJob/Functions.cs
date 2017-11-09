using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace Elmah.Io.Functions.WebJob
{
    [ElmahIoExceptionFilter("API_KEY", "LOG_ID")]
    public class Functions
    {
        // This function will get triggered/executed when a new message is written 
        // on an Azure Queue called queue.
        public static void Trigger([TimerTrigger("0 */1 * * * *")]TimerInfo myTimer, TextWriter log)
        {
            throw new ApplicationException("Error from webjob");
        }
    }
}
