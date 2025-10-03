using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using NServiceBus;
var builder = FunctionsApplication.CreateBuilder(args);

//builder.AddNServiceBus2(c =>
//{
//    c.Routing.RouteToEndpoint(typeof(TriggerMessage), "FunctionsTestEndpoint2");
//    c.AdvancedConfiguration.EnableInstallers();
//});

var host = builder.Build();

await host.RunAsync();
