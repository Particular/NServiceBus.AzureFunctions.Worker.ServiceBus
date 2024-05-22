namespace NServiceBus.AzureFunctions.Worker.ServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
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

        public async Task Process(
            byte[] body,
            IDictionary<string, object> userProperties,
            string messageId,
            int deliveryCount,
            string replyTo,
            string correlationId,
            ITransactionStrategy transactionStrategy,
            CancellationToken cancellationToken = default)
        {
            body ??= Array.Empty<byte>(); // might be null
            messageId ??= Guid.NewGuid().ToString("N");

            try
            {
                using (var transaction = transactionStrategy.CreateTransaction())
                {
                    var transportTransaction = transactionStrategy.CreateTransportTransaction(transaction);
                    var messageContext = new MessageContext(
                        messageId,
                        CreateNServiceBusHeaders(userProperties, replyTo, correlationId),
                        body,
                        transportTransaction,
                        ReceiveAddress,
                        new ContextBag());

                    await onMessage(messageContext, cancellationToken).ConfigureAwait(false);

                    await transactionStrategy.Complete(transaction, cancellationToken).ConfigureAwait(false);

                    transaction?.Commit();
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                using (var transaction = transactionStrategy.CreateTransaction())
                {
                    var transportTransaction = transactionStrategy.CreateTransportTransaction(transaction);
                    var errorContext = new ErrorContext(
                        exception,
                        CreateNServiceBusHeaders(userProperties, replyTo, correlationId),
                        messageId,
                        body,
                        transportTransaction,
                        deliveryCount,
                        ReceiveAddress,
                        new ContextBag());

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

            static Dictionary<string, string> CreateNServiceBusHeaders(IDictionary<string, object> userProperties, string replyTo, string correlationId)
            {
                var headers = new Dictionary<string, string>(userProperties.Count);

                foreach (var userProperty in userProperties)
                {
                    headers[userProperty.Key] = userProperty.Value?.ToString();
                }

                headers.Remove("NServiceBus.Transport.Encoding");

                if (!string.IsNullOrWhiteSpace(replyTo))
                {
                    headers.TryAdd(Headers.ReplyToAddress, replyTo);
                }

                if (!string.IsNullOrWhiteSpace(correlationId))
                {
                    headers.TryAdd(Headers.CorrelationId, correlationId);
                }

                return headers;
            }
        }

        public Task StartReceive(CancellationToken cancellationToken = default) => Task.CompletedTask;

        // No-op because the rate at which Azure Functions pushes messages to the pipeline can't be controlled.
        public Task ChangeConcurrency(PushRuntimeSettings limitations, CancellationToken cancellationToken = new CancellationToken()) => Task.CompletedTask;

        public Task StopReceive(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public ISubscriptionManager Subscriptions => baseTransportReceiver.Subscriptions;
        public string Id => baseTransportReceiver.Id;
        public string ReceiveAddress => baseTransportReceiver.ReceiveAddress;

        OnMessage onMessage;
        OnError onError;
    }
}