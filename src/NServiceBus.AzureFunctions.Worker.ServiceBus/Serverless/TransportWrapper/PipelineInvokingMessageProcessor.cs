namespace NServiceBus.AzureFunctions.Worker.ServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Messaging.ServiceBus;
    using Microsoft.Azure.Functions.Worker;
    using NServiceBus.Extensibility;
    using Transport;
    using Transport.AzureServiceBus;

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
            CancellationToken cancellationToken = default)
        {
            var messageId = message.GetMessageId();
            var body = message.GetBody();
            var contextBag = new ContextBag();
            contextBag.Set(message);

            try
            {
                using var azureServiceBusTransportTransaction = new AzureServiceBusTransportTransaction();
                var messageContext = CreateMessageContext(message, messageId, body, azureServiceBusTransportTransaction.TransportTransaction, contextBag);

                await onMessage(messageContext, cancellationToken).ConfigureAwait(false);

                azureServiceBusTransportTransaction.Commit();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                using var azureServiceBusTransportTransaction = new AzureServiceBusTransportTransaction();
                var errorContext = CreateErrorContext(message, exception, messageId, body, azureServiceBusTransportTransaction.TransportTransaction, contextBag);

                var errorHandleResult = await onError.Invoke(errorContext, cancellationToken).ConfigureAwait(false);

                if (errorHandleResult == ErrorHandleResult.Handled)
                {
                    azureServiceBusTransportTransaction.Commit();
                    return;
                }

                throw;
            }
        }

        ErrorContext CreateErrorContext(ServiceBusReceivedMessage message, Exception exception, string messageId,
            BinaryData body, TransportTransaction transportTransaction, ContextBag contextBag) =>
            new(exception, message.GetNServiceBusHeaders(), messageId, body, transportTransaction, message.DeliveryCount, ReceiveAddress, contextBag);

        MessageContext CreateMessageContext(ServiceBusReceivedMessage message, string messageId, BinaryData body,
            TransportTransaction transportTransaction, ContextBag contextBag) =>
            new(messageId, message.GetNServiceBusHeaders(), body, transportTransaction, ReceiveAddress, contextBag);

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