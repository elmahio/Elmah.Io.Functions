# Elmah.Io.Functions

Log to [elmah.io](https://elmah.io/) from Azure Functions and WebJobs.

## Installation
Elmah.Io.Functions installs through NuGet:

```
PS> Install-Package Elmah.Io.Functions -Pre
```

Configure the elmah.io exception filter through code:

```csharp
[ElmahIoExceptionFilter("API_KEY", "LOG_ID")]
public static class FailingFunction
{
    [FunctionName("FailingFunction")]
    public static void Run([TimerTrigger("0 */1 * * * *")]TimerInfo myTimer, TraceWriter log)
    {
        throw new ApplicationException("Error happened during function");
    }
}
```

Replace `API_KEY` with your API key and `LOG_ID` with your log ID.