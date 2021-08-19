using Microsoft.Extensions.Hosting;

namespace test
{
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