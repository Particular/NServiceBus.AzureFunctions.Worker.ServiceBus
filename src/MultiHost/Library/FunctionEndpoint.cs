namespace MultiHost;

using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;

// In the current version this object does send and receive but technically we could now totally split this responsibility
// and have a FunctionReceiver and a FunctionSender or just use IMessageSession with a sendonly endpoint or do we foresee
// specific routing being necessary per function?
public sealed class FunctionEndpoint(Func<ServiceBusReceivedMessage, ServiceBusMessageActions, CancellationToken, Task> processor)
{
    public async Task Process(ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions,
        CancellationToken cancellationToken = default)
    {
        await processor(message, messageActions, cancellationToken).ConfigureAwait(false);
        await Console.Out
            .WriteLineAsync($"Processing message: {message.MessageId}").ConfigureAwait(false);
    }
}