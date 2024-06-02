namespace NServiceBus.AzureFunctions.Worker.ServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Messaging.ServiceBus;

    class SendOnlyMessageProcessor : IMessageProcessor
    {
        public Task Process(ServiceBusReceivedMessage serviceBusReceivedMessage,
            ITransactionStrategy transactionStrategy,
            CancellationToken cancellationToken = default) => throw new InvalidOperationException(
            $"This endpoint cannot process messages because it is configured in send-only mode. Remove the '{nameof(EndpointConfiguration)}.{nameof(EndpointConfiguration.SendOnly)}' configuration.'"
        );
    }
}