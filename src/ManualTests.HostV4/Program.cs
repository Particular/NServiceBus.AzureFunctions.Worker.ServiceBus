using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NServiceBus;

[assembly: NServiceBusTriggerFunction("%MY_ENDPOINT_NAME%", TriggerFunctionName = "MyFunctionName")]

public class Program
{
    public static void Main()
    {
        var host = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults()            
            .UseNServiceBus(c =>
            {
                c.AdvancedConfiguration.SendOnly();

                c.Routing.RouteToEndpoint(typeof(TriggerMessage), "some-queue");
            })
            .ConfigureServices(s => s.AddHostedService<MyHostedService>())
            .Build();

        host.Run();
    }

    class MyHostedService : IHostedService
    {
        IFunctionEndpoint functionEndpoint;

        public MyHostedService(IFunctionEndpoint functionEndpoint)
        {
            this.functionEndpoint = functionEndpoint;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}