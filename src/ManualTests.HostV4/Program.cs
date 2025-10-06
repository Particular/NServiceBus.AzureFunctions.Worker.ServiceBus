using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using NServiceBus;

var builder = FunctionsApplication.CreateBuilder(args);

builder.AddNServiceBus2(c =>
{
    c.UseSerialization<SystemJsonSerializer>();
});

var host = builder.Build();

await host.RunAsync();
