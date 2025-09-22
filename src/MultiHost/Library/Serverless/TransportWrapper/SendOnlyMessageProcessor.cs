namespace NServiceBus.AzureFunctions.Worker.ServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;

class SendOnlyMessageProcessor : IMessageProcessor
{
    public Task Process(ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions,
        CancellationToken cancellationToken = default) => throw new InvalidOperationException(
        $"This endpoint cannot process messages because it is configured in send-only mode. Remove the '{nameof(EndpointConfiguration)}.{nameof(EndpointConfiguration.SendOnly)}' configuration.'"
    );
}