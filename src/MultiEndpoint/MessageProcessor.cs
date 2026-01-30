using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using MultiEndpoint.Logging;
using NServiceBus.AzureFunctions.Worker.ServiceBus;

class MessageProcessor(AzureServiceBusServerlessTransport transport, EndpointStarter endpointStarter) : IMessageProcessor
{
    public async Task Process(ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions,
        FunctionContext functionContext, CancellationToken cancellationToken = default)
    {
        using var _ = FunctionsLoggerFactory.Instance.PushName(endpointStarter.ServiceKey);
        await endpointStarter.GetOrStart(cancellationToken).ConfigureAwait(false);
        await transport.MessageProcessor.Process(message, messageActions, cancellationToken).ConfigureAwait(false);
    }
}