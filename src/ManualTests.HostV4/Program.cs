using Microsoft.Extensions.Hosting;
using NServiceBus;

[assembly: NServiceBusTriggerFunction("FunctionsTestEndpoint2", TriggerFunctionName = "MyFunctionName")]

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .UseNServiceBus(c =>
    {
        c.Routing.RouteToEndpoint(typeof(TriggerMessage), "FunctionsTestEndpoint2");
        c.AdvancedConfiguration.EnableInstallers();
    })
    .Build();

host.Run();
