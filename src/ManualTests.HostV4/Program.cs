using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using NServiceBus;

[assembly: NServiceBusTriggerFunction("FunctionsTestEndpoint2", TriggerFunctionName = "MyFunctionName")]

var builder = FunctionsApplication.CreateBuilder(args);

builder.AddNServiceBus(c =>
{
    c.Routing.RouteToEndpoint(typeof(TriggerMessage), "FunctionsTestEndpoint2");
    c.AdvancedConfiguration.EnableInstallers();
});

var host = builder.Build();

await host.RunAsync();
