using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;

namespace MultiEndpoint;

public class ReceiverEndpoint([FromKeyedServices("ReceiverEndpoint")] IMessageProcessor processor)
{
    [Function("ReceiverEndpoint")]
    public Task Receiver(
        [ServiceBusTrigger("ReceiverEndpoint", Connection = "AzureWebJobsServiceBus", AutoCompleteMessages = false)]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, FunctionContext context, CancellationToken cancellationToken = default)
    {
        return processor.Process(message, messageActions, context, cancellationToken);
    }
}