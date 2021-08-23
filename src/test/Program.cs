//using NServiceBus;

//[assembly: NServiceBusTriggerFunction("ASBTriggerQueue", TriggerFunctionName = "TestFunction")]

namespace test
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using NServiceBus;

    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices(collection => collection.AddLogging())
                .UseNServiceBus(() => new ServiceBusTriggeredEndpointConfiguration("ASBTriggerQueue"))
                .Build();

            host.Run();
        }
    }
}