using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;

namespace MultiEndpoint;

public class ReceiverEndpoint
{
    [Function("ReceiverEndpoint")]
    public Task Receiver(
        [ServiceBusTrigger("ReceiverEndpoint", Connection = "AzureWebJobsServiceBus", AutoCompleteMessages = false)]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, FunctionContext context, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}