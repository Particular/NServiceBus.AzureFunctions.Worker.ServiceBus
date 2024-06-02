namespace NServiceBus.AzureFunctions.Worker.ServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Messaging.ServiceBus;

    interface IMessageProcessor
    {
        Task Process(
            ServiceBusReceivedMessage serviceBusReceivedMessage,
            ITransactionStrategy transactionStrategy,
            CancellationToken cancellationToken = default);
    }
}