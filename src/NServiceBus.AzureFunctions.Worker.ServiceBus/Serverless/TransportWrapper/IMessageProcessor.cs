namespace NServiceBus.AzureFunctions.Worker.ServiceBus
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    interface IMessageProcessor
    {
        Task Process(
            byte[] body,
            IDictionary<string, object> userProperties,
            string messageId,
            int deliveryCount,
            string replyTo,
            string correlationId,
            ITransactionStrategy transactionStrategy,
            CancellationToken cancellationToken = default);
    }
}