using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MultiHost;

FunctionsApplicationBuilder builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();

builder.Services.AddApplicationInsightsTelemetryWorkerService();
builder.Services.ConfigureFunctionsApplicationInsights();
builder.Services.AddNServiceBus(configuration =>
{
    // these setting will be applied to all functions
    configuration.UsePersistence<LearningPersistence>();
    //configuration.UseTransport()//this should not be done at this level
});

IHost host = builder.Build();

await host.RunAsync().ConfigureAwait(false);