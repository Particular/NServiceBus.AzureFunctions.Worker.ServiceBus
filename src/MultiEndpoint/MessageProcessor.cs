using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using NServiceBus.AzureFunctions.Worker.ServiceBus;

class MessageProcessor(ServerlessTransport transport, EndpointStarter endpointStarter) : IMessageProcessor
{
    public async Task Process(ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions,
        FunctionContext functionContext, CancellationToken cancellationToken = default)
    {
        _ = await endpointStarter.GetOrStart(cancellationToken).ConfigureAwait(false);
        await transport.MessageProcessor.Process(message, messageActions, cancellationToken);
    }
}