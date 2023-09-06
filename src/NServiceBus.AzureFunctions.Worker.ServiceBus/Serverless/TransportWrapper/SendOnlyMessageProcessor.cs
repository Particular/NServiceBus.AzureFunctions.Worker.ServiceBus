namespace NServiceBus.AzureFunctions.Worker.ServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    class SendOnlyMessageProcessor : IMessageProcessor
    {
        public Task Process(
            byte[] body,
            IDictionary<string, object> userProperties,
            string messageId,
            int deliveryCount,
            string replyTo,
            string correlationId,
            ITransactionStrategy transactionStrategy,
            CancellationToken cancellationToken)
                => throw new InvalidOperationException(
                    $"This endpoint cannot process messages because it is configured in send-only mode. Remove the '{nameof(EndpointConfiguration)}.{nameof(EndpointConfiguration.SendOnly)}' configuration.'"
                    );
    }
}