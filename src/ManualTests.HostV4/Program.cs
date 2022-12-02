using Microsoft.Extensions.Hosting;
using NServiceBus;

[assembly: NServiceBusTriggerFunction("%MY_ENDPOINT_NAME%", TriggerFunctionName = "MyFunctionName")]

public class Program
{
    public static void Main()
    {
        var host = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults()
            .UseNServiceBus()
            .Build();

        host.Run();
    }
}