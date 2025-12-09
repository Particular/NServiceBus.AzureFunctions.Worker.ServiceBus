using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MultiEndpoint.Services;
using NServiceBus.AzureFunctions.Worker.ServiceBus;
using NServiceBus.TransactionalSession;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Sender();
builder.Receiver();

// builder.AddNServiceBus(c =>
// {
//     c.Routing.RouteToEndpoint(typeof(TriggerMessage), "FunctionsTestEndpoint2");
//     c.AdvancedConfiguration.EnableInstallers();
// });

var host = builder.Build();

await host.RunAsync();

public static class SenderEndpointConfigurationExtensions
{
    public static void Sender(this FunctionsApplicationBuilder builder)
    {
        builder.Services.AddAzureClientsCore();

        var endpointConfiguration = new EndpointConfiguration("SenderEndpoint");
        endpointConfiguration.SendOnly();
        endpointConfiguration.EnableOutbox();

        var persistence = endpointConfiguration.UsePersistence<MongoPersistence>();
        persistence.EnableTransactionalSession(new TransactionalSessionOptions
        {
            ProcessorEndpoint = "ReceiverEndpoint"
        });
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();

        var transport = new AzureServiceBusTransport("TransportWillBeInitializedCorrectlyLater", TopicTopology.Default)
        {
            TransportTransactionMode = TransportTransactionMode.ReceiveOnly
        };
        var serverlessTransport = new ServerlessTransport(transport, null, "AzureWebJobsServiceBus");
        endpointConfiguration.UseTransport(serverlessTransport);

        var keyedServices = new KeyedServiceCollectionAdapter(builder.Services, "SenderEndpoint");
        var startableEndpoint = EndpointWithExternallyManagedContainer.Create(
            endpointConfiguration,
            keyedServices);

        builder.Services.AddHostedService(s=> new InitializationService("SenderEndpoint", keyedServices, s, startableEndpoint, serverlessTransport));
        builder.Services.AddKeyedSingleton<IMessageSession>("SenderEndpoint", (_, __) => startableEndpoint.MessageSession.Value);
    }
}

public static class ReceiverEndpointConfigurationExtensions
{
    public static void Receiver(this FunctionsApplicationBuilder builder)
    {
        builder.Services.AddAzureClientsCore();

        var endpointConfiguration = new EndpointConfiguration("ReceiverEndpoint");
        endpointConfiguration.EnableOutbox();
        var persistence = endpointConfiguration.UsePersistence<MongoPersistence>();
        persistence.EnableTransactionalSession();

        endpointConfiguration.UseSerialization<SystemJsonSerializer>();

        var transport = new AzureServiceBusTransport("TransportWillBeInitializedCorrectlyLater", TopicTopology.Default)
        {
            TransportTransactionMode = TransportTransactionMode.ReceiveOnly
        };
        var serverlessTransport = new ServerlessTransport(transport, null, "AzureWebJobsServiceBus");
        endpointConfiguration.UseTransport(serverlessTransport);

        var keyedServices = new KeyedServiceCollectionAdapter(builder.Services, "ReceiverEndpoint");
        var startableEndpoint = EndpointWithExternallyManagedContainer.Create(
            endpointConfiguration,
            keyedServices);

        builder.Services.AddHostedService(s=> new InitializationService("ReceiverEndpoint", keyedServices, s, startableEndpoint, serverlessTransport));
        builder.Services.AddKeyedSingleton<IMessageSession>("ReceiverEndpoint", (_, __) => startableEndpoint.MessageSession.Value);
    }
}

class InitializationService(
    string serviceKey,
    KeyedServiceCollectionAdapter services,
    IServiceProvider provider,
    IStartableEndpointWithExternallyManagedContainer startableEndpoint,
    ServerlessTransport serverlessTransport) : IHostedService
{
    private IEndpointInstance? endpointInstance;
    private KeyedServiceProviderAdapter? keyedServices;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        keyedServices = new KeyedServiceProviderAdapter(provider, serviceKey, services);
        serverlessTransport.ServiceProvider = keyedServices;

        endpointInstance = await startableEndpoint.Start(keyedServices, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (endpointInstance != null && keyedServices != null)
        {
            await endpointInstance.Stop(cancellationToken);
            await keyedServices.DisposeAsync();
        }
    }
}