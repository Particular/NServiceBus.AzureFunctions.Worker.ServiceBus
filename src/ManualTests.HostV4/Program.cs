using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.AddNServiceBus2(c =>
{
});

var host = builder.Build();

await host.RunAsync();
