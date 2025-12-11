using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;

public interface IMessageProcessor
{
    Task Process(ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions, FunctionContext functionContext, CancellationToken cancellationToken = default);
}