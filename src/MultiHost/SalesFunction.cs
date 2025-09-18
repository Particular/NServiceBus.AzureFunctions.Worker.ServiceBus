namespace MultiHost;

using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.AzureFunctions.Worker.ServiceBus;

public partial class SalesFunction
{
    [Function(nameof(SalesFunction))]
    public partial Task Sales(
        [ServiceBusTrigger("sales", Connection = "ServiceBusConnection", AutoCompleteMessages = false)]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, CancellationToken cancellationToken = default);

    public void Configure(EndpointConfiguration endpointConfiguration)
    {
        endpointConfiguration.AddHandler<OrderAcceptedHandler>();
    }
}

public partial class SalesFunction([FromKeyedServices(nameof(SalesFunction))] FunctionEndpoint endpoint) : IConfigureEndpoint
{
    public async partial Task Sales(ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, CancellationToken cancellationToken = default)
    {
        await endpoint.Process(message, messageActions, this, cancellationToken).ConfigureAwait(false);
    }
}

// Maybe not even needed if we are clever or we only implement it when the function enpoint has a COnfigure method
public interface IConfigureEndpoint
{
    void Configure(EndpointConfiguration endpointConfiguration);
}

public class FunctionEndpoint
{
    IMessageProcessor? messageProcessor;
    SemaphoreSlim semaphoreLock;

    public FunctionEndpoint(CommonEndpointConfigurationProvider configurationProvider)
    {

    }

    public async Task Process(ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions, IConfigureEndpoint endpoint = null,
        CancellationToken cancellationToken = default) => await Console.Out
        .WriteLineAsync($"Processing message: {message.MessageId}").ConfigureAwait(false);

    internal async Task InitializeEndpointIfNecessary(CancellationToken cancellationToken = default)
    {
        if (messageProcessor == null)
        {
            await semaphoreLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (messageProcessor == null)
                {
                    endpoint = await endpointFactory().ConfigureAwait(false);

                    messageProcessor = serverlessTransport.MessageProcessor;
                }
            }
            finally
            {
                semaphoreLock.Release();
            }
        }
    }
}