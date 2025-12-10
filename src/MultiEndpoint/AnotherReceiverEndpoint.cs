using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;

namespace MultiEndpoint;

public class AnotherReceiverEndpoint([FromKeyedServices("AnotherReceiverEndpoint")] IMessageProcessor processor)
{
    [Function("AnotherReceiverEndpoint")]
    public Task Receiver(
        [ServiceBusTrigger("AnotherReceiverEndpoint", Connection = "AzureWebJobsServiceBus", AutoCompleteMessages = false)]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, FunctionContext context, CancellationToken cancellationToken = default)
    {
        return processor.Process(message, messageActions, context, cancellationToken);
    }
}