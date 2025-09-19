using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MultiHost;
using NServiceBus.Configuration.AdvancedExtensibility;

FunctionsApplicationBuilder builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();

builder.Services.AddApplicationInsightsTelemetryWorkerService();
builder.Services.ConfigureFunctionsApplicationInsights();
builder.Services.AddNServiceBus(configuration =>
{
    configuration.UsePersistence<LearningPersistence>();
});

IHost host = builder.Build();

await host.RunAsync().ConfigureAwait(false);

public static class EndpointConfigurationExtensions
{
    public static void AddHandler<THandler>(this EndpointConfiguration endpointConfiguration) => endpointConfiguration
        .GetSettings().GetOrCreate<RegisteredHandlers>().Add(typeof(THandler));

    public static void AddSaga<TSaga>(this EndpointConfiguration endpointConfiguration)
        where TSaga : Saga
        => endpointConfiguration
        .GetSettings().GetOrCreate<RegisteredSagas>().Add(typeof(TSaga));

    internal class RegisteredHandlers
    {
        public HashSet<Type> HandlerTypes { get; } = [];

        public void Add(Type handlerType) => HandlerTypes.Add(handlerType);
    }

    internal class RegisteredSagas
    {
        public HashSet<Type> SagaTypes { get; } = [];

        public void Add(Type handlerType) => SagaTypes.Add(handlerType);
    }
}