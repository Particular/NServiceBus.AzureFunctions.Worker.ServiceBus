namespace MultiHost;

using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.AzureFunctions.Worker.ServiceBus;
using NServiceBus.Configuration.AdvancedExtensibility;

public partial class BillingFunction
{
    [Function(nameof(BillingFunction))]
    public partial Task Billing(
        [ServiceBusTrigger("billing", Connection = "ServiceBusConnection", AutoCompleteMessages = false)]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, CancellationToken cancellationToken = default);

    public void Configure(AzureServiceBusTransport transport, RoutingSettings<AzureServiceBusTransport> routing, EndpointConfiguration endpointConfiguration)
    {
        endpointConfiguration.AddHandler<OrderAcceptedHandler>();
    }
}

public partial class SalesFunction
{
    [Function(nameof(SalesFunction))]
    public partial Task Sales(
        [ServiceBusTrigger("sales", Connection = "ServiceBusConnection", AutoCompleteMessages = false)]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, CancellationToken cancellationToken = default);

    public void Configure(AzureServiceBusTransport transport, RoutingSettings<AzureServiceBusTransport> routing, EndpointConfiguration endpointConfiguration)
    {
        endpointConfiguration.AddHandler<PlaceOrderHandler>();
        endpointConfiguration.AddSaga<OrderFullfillmentPolicy>();

        routing.RouteToEndpoint(typeof(PlaceOrder), "sales");
    }
}

public partial class BillingFunction([FromKeyedServices(nameof(BillingFunction))] FunctionEndpoint endpoint) : IConfigureEndpoint
{
    public async partial Task Billing(ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, CancellationToken cancellationToken = default)
    {
        // Because we generate this you can essentially add a decorator chain of IConfigureEndpoint implementations
        // that for example adds specific stuff to the endpoint configuration like source generator discovered handlers
        // and other things.
        await endpoint.Process(message, messageActions, this as IConfigureEndpoint, cancellationToken).ConfigureAwait(false);
    }
}

public partial class SalesFunction([FromKeyedServices(nameof(SalesFunction))] FunctionEndpoint endpoint) : IConfigureEndpoint
{
    public async partial Task Sales(ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, CancellationToken cancellationToken = default)
    {
        // Because we generate this you can essentially add a decorator chain of IConfigureEndpoint implementations
        // that for example adds specific stuff to the endpoint configuration like source generator discovered handlers
        // and other things.
        await endpoint.Process(message, messageActions, this as IConfigureEndpoint, cancellationToken).ConfigureAwait(false);
    }
}

// Maybe not even needed if we are clever or we only implement it when the function enpoint has a COnfigure method
public interface IConfigureEndpoint
{
    void Configure(AzureServiceBusTransport transport, RoutingSettings<AzureServiceBusTransport> routing, EndpointConfiguration endpointConfiguration);
}

// In the current version this object does send and receive but technically we could now totally split this responsibility
// and have a FunctionReceiver and a FunctionSender or just use IMessageSession with a sendonly endpoint or do we foresee
// specific routing being necessary per function?
public sealed class FunctionEndpoint(string functionName, Action<EndpointConfiguration> customizeConfig) : IAsyncDisposable
{
    IMessageProcessor? messageProcessor;
    SemaphoreSlim semaphoreLock = new(1, 1);
    ServiceProvider serviceProvider;
    IEndpointInstance endpoint;

    public async Task Process(ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions, IConfigureEndpoint endpoint = null,
        CancellationToken cancellationToken = default) => await Console.Out
        .WriteLineAsync($"Processing message: {message.MessageId}").ConfigureAwait(false);

    // The semaphore lock crap can also be avoided by also emitting a hosted service that does the initialization
    // this is just for demonstration purposes
    // Benefits you can immediate feedback when you have configuration issues.
    internal async Task InitializeEndpointIfNecessary(IConfigureEndpoint configureEndpoint, CancellationToken cancellationToken = default)
    {
        if (messageProcessor == null)
        {
            await semaphoreLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (messageProcessor == null)
                {
                    var transport = new AzureServiceBusTransport("TBD", TopicTopology.Default);
                    var endpointConfiguration = new EndpointConfiguration(functionName);
                    // Here we have access to the full service provider and can basically achieve almost anything
                    // so the serverless transport can get less complicated

                    // Dependencies registered here will be available in the function endpoint handlers
                    customizeConfig(endpointConfiguration);
                    configureEndpoint.Configure(transport, new RoutingSettings<AzureServiceBusTransport>(endpointConfiguration.GetSettings()), endpointConfiguration);

                    // here we can cobble out types out of the settings and configure it properly

                    var serviceCollection = new ServiceCollection();

                    var startableEndpoint = EndpointWithExternallyManagedContainer.Create(endpointConfiguration, serviceCollection);

                    serviceProvider = serviceCollection.BuildServiceProvider();

                    endpoint = await startableEndpoint.Start(serviceProvider, cancellationToken: cancellationToken).ConfigureAwait(false);

                    messageProcessor = serverlessTransport.MessageProcessor;
                }
            }
            finally
            {
                semaphoreLock.Release();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        // needs proper implementation
        await endpoint.Stop().ConfigureAwait(false);
        await serviceProvider.DisposeAsync().ConfigureAwait(false);
    }
}