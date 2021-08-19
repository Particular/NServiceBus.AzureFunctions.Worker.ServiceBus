using NServiceBus;

[assembly: NServiceBusTriggerFunction("ASBTriggerQueue")]

namespace test
{
    using Microsoft.Extensions.Hosting;
    using NServiceBus;

    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .UseNServiceBus(() => new ServiceBusTriggeredEndpointConfiguration("ASBTriggerQueue"))
                .Build();

            host.Run();
        }
    }
}