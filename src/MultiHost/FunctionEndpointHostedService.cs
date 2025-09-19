namespace MultiHost;

using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NServiceBus.Configuration.AdvancedExtensibility;

public sealed class FunctionEndpointHostedService<TContext>(
    string functionName,
    Action<EndpointConfiguration> customizeConfig,
    IConfigureEndpoint configureEndpoint) : IHostedService, IAsyncDisposable
    where TContext : InitializationContext, new()
{
    ServiceProvider serviceProvider;
    IEndpointInstance endpoint;

    public Func<ServiceBusReceivedMessage, ServiceBusMessageActions, CancellationToken, Task> Processor { get; private set; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var serviceCollection = new ServiceCollection();

        var transport = new AzureServiceBusTransport("TBD", TopicTopology.Default);
        var endpointConfiguration = new EndpointConfiguration(functionName);
        // Here we have access to the full service provider and can basically achieve almost anything
        // so the serverless transport can get less complicated

        // Dependencies registered here will be available in the function endpoint handlers
        customizeConfig(endpointConfiguration);
        configureEndpoint.Configure(new TContext
        {
            Configuration = endpointConfiguration,
            Transport = transport,
            Routing = new RoutingSettings<AzureServiceBusTransport>(endpointConfiguration.GetSettings()),
            Services = serviceCollection
        });

        // here we can cobble out types out of the settings and configure it properly

        var startableEndpoint = EndpointWithExternallyManagedContainer.Create(endpointConfiguration, serviceCollection);

        serviceProvider = serviceCollection.BuildServiceProvider();

        endpoint = await startableEndpoint.Start(serviceProvider, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        //Processor = new ServerlessTransport(transport,"TBD", "TBD").MessageProcessor;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await endpoint.Stop(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        await serviceProvider.DisposeAsync().ConfigureAwait(false);
    }
}