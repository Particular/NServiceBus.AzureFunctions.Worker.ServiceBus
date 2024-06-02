namespace NServiceBus.AzureFunctions.Worker.ServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Messaging.ServiceBus;
    using Microsoft.Azure.Functions.Worker;
    using NServiceBus.Extensibility;
    using Transport;

    class PipelineInvokingMessageProcessor(IMessageReceiver baseTransportReceiver) : IMessageReceiver, IMessageProcessor
    {
        public Task Initialize(PushRuntimeSettings limitations, OnMessage onMessage, OnError onError,
            CancellationToken cancellationToken = default)
        {
            this.onMessage = onMessage;
            this.onError = onError;
            return baseTransportReceiver?.Initialize(limitations,
                (_, __) => Task.CompletedTask,
                (_, __) => Task.FromResult(ErrorHandleResult.Handled),
                cancellationToken) ?? Task.CompletedTask;
        }

        public async Task Process(ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions,
            ITransactionStrategy transactionStrategy,
            CancellationToken cancellationToken = default)
        {
            var messageId = message.GetMessageId();
            var body = message.GetBody();
            var contextBag = new ContextBag();
            contextBag.Set(message);

            try
            {
                using var transaction = transactionStrategy.CreateTransaction();
                var transportTransaction = transactionStrategy.CreateTransportTransaction(transaction);
                var messageContext = new MessageContext(
                    messageId,
                    message.GetNServiceBusHeaders(),
                    body,
                    transportTransaction,
                    ReceiveAddress,
                    contextBag);

                await onMessage(messageContext, cancellationToken).ConfigureAwait(false);

                await transactionStrategy.Complete(transaction, cancellationToken).ConfigureAwait(false);

                transaction?.Commit();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                using var transaction = transactionStrategy.CreateTransaction();
                var transportTransaction = transactionStrategy.CreateTransportTransaction(transaction);
                var errorContext = new ErrorContext(
                    exception,
                    message.GetNServiceBusHeaders(),
                    messageId,
                    body,
                    transportTransaction,
                    message.DeliveryCount,
                    ReceiveAddress,
                    contextBag);

                var errorHandleResult = await onError.Invoke(errorContext, cancellationToken).ConfigureAwait(false);

                if (errorHandleResult == ErrorHandleResult.Handled)
                {
                    await transactionStrategy.Complete(transaction, cancellationToken).ConfigureAwait(false);

                    transaction?.Commit();
                    return;
                }

                throw;
            }
        }

        public Task StartReceive(CancellationToken cancellationToken = default) => Task.CompletedTask;

        // No-op because the rate at which Azure Functions pushes messages to the pipeline can't be controlled.
        public Task ChangeConcurrency(PushRuntimeSettings limitations, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task StopReceive(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public ISubscriptionManager Subscriptions => baseTransportReceiver.Subscriptions;
        public string Id => baseTransportReceiver.Id;
        public string ReceiveAddress => baseTransportReceiver.ReceiveAddress;

        OnMessage onMessage;
        OnError onError;
    }
}