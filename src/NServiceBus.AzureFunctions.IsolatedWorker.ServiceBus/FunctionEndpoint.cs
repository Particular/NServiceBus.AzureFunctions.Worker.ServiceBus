namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AzureFunctions.InProcess.ServiceBus;
    using Extensibility;
    using Microsoft.Azure.Functions.Worker;
    using Transport;

    /// <summary>
    /// An NServiceBus endpoint hosted in Azure Function which does not receive messages automatically but only handles
    /// messages explicitly passed to it by the caller.
    /// </summary>
    public class FunctionEndpoint : IFunctionEndpoint
    {
        // This ctor is used for the FunctionsHostBuilder scenario where the endpoint is created already during configuration time using the function host's container.
        internal FunctionEndpoint(IStartableEndpointWithExternallyManagedContainer externallyManagedContainerEndpoint, ServiceBusTriggeredEndpointConfiguration configuration, IServiceProvider serviceProvider)
        {
            this.configuration = configuration;
            endpointFactory = _ => externallyManagedContainerEndpoint.Start(serviceProvider);
        }

        /// <inheritdoc />
        public async Task Process(
            byte[] body,
            IDictionary<string, string> userProperties,
            string messageId,
            int deliveryCount,
            string replyTo,
            string correlationId,
            FunctionContext functionContext)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionContext.GetLogger("NServiceBus"));

            await InitializeEndpointIfNecessary(functionContext, CancellationToken.None)
                .ConfigureAwait(false);

            await Process(body, userProperties, messageId, deliveryCount, replyTo, correlationId, NoTransactionStrategy.Instance, pipeline)
                .ConfigureAwait(false);
        }

        internal static async Task Process(
            byte[] body,
            IDictionary<string, string> userProperties,
            string messageId,
            int deliveryCount,
            string replyTo,
            string correlationId,
            ITransactionStrategy transactionStrategy,
            PipelineInvoker pipeline)
        {
            body ??= new byte[0]; // might be null
            messageId ??= Guid.NewGuid().ToString("N");

            try
            {
                using (var transaction = transactionStrategy.CreateTransaction())
                {
                    var transportTransaction = transactionStrategy.CreateTransportTransaction(transaction);
                    var messageContext = CreateMessageContext(transportTransaction);

                    await pipeline.PushMessage(messageContext).ConfigureAwait(false);

                    await transactionStrategy.Complete(transaction).ConfigureAwait(false);

                    transaction?.Commit();
                }
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
                        new ContextBag());

                    var errorHandleResult = await pipeline.PushFailedMessage(errorContext).ConfigureAwait(false);

                    if (errorHandleResult == ErrorHandleResult.Handled)
                    {
                        await transactionStrategy.Complete(transaction).ConfigureAwait(false);

                        transaction?.Commit();
                        return;
                    }

                    throw;
                }
            }

            MessageContext CreateMessageContext(TransportTransaction transportTransaction) =>
                new MessageContext(
                    messageId,
                    CreateNServiceBusHeaders(userProperties, replyTo, correlationId),
                    body,
                    transportTransaction,
                    new CancellationTokenSource(),
                    new ContextBag());
        }

        async Task InitializeEndpointIfNecessary(FunctionContext functionContext, CancellationToken cancellationToken)
        {
            if (pipeline == null)
            {
                await semaphoreLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    if (pipeline == null)
                    {
                        endpoint = await endpointFactory(functionContext).ConfigureAwait(false);

                        pipeline = configuration.PipelineInvoker;
                    }
                }
                finally
                {
                    semaphoreLock.Release();
                }
            }
        }

        /// <inheritdoc />
        public async Task Send(object message, SendOptions options, FunctionContext executionContext)
        {
            await InitializeEndpointUsedOutsideHandlerIfNecessary(executionContext).ConfigureAwait(false);

            await endpoint.Send(message, options).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task Send(object message, FunctionContext executionContext)
        {
            return Send(message, new SendOptions(), executionContext);
        }

        /// <inheritdoc />
        public async Task Send<T>(Action<T> messageConstructor, SendOptions options, FunctionContext executionContext)
        {
            await InitializeEndpointUsedOutsideHandlerIfNecessary(executionContext).ConfigureAwait(false);

            await endpoint.Send(messageConstructor, options).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task Send<T>(Action<T> messageConstructor, FunctionContext executionContext)
        {
            return Send(messageConstructor, new SendOptions(), executionContext);
        }

        /// <inheritdoc />
        public async Task Publish(object message, PublishOptions options, FunctionContext executionContext)
        {
            await InitializeEndpointUsedOutsideHandlerIfNecessary(executionContext).ConfigureAwait(false);

            await endpoint.Publish(message, options).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task Publish<T>(Action<T> messageConstructor, PublishOptions options, FunctionContext executionContext)
        {
            await InitializeEndpointUsedOutsideHandlerIfNecessary(executionContext).ConfigureAwait(false);

            await endpoint.Publish(messageConstructor, options).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task Publish(object message, FunctionContext executionContext)
        {
            await InitializeEndpointUsedOutsideHandlerIfNecessary(executionContext).ConfigureAwait(false);

            await endpoint.Publish(message).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task Publish<T>(Action<T> messageConstructor, FunctionContext executionContext)
        {
            await InitializeEndpointUsedOutsideHandlerIfNecessary(executionContext).ConfigureAwait(false);

            await endpoint.Publish(messageConstructor).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task Subscribe(Type eventType, SubscribeOptions options, FunctionContext executionContext)
        {
            await InitializeEndpointUsedOutsideHandlerIfNecessary(executionContext).ConfigureAwait(false);

            await endpoint.Subscribe(eventType, options).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task Subscribe(Type eventType, FunctionContext executionContext)
        {
            await InitializeEndpointUsedOutsideHandlerIfNecessary(executionContext).ConfigureAwait(false);

            await endpoint.Subscribe(eventType).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task Unsubscribe(Type eventType, UnsubscribeOptions options, FunctionContext executionContext)
        {
            await InitializeEndpointUsedOutsideHandlerIfNecessary(executionContext).ConfigureAwait(false);

            await endpoint.Unsubscribe(eventType, options).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task Unsubscribe(Type eventType, FunctionContext executionContext)
        {
            await InitializeEndpointUsedOutsideHandlerIfNecessary(executionContext).ConfigureAwait(false);

            await endpoint.Unsubscribe(eventType).ConfigureAwait(false);
        }

        async Task InitializeEndpointUsedOutsideHandlerIfNecessary(FunctionContext functionContext)
        {
            //TODO might also pass the logger factory instead of using a single logger category
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionContext.GetLogger("NServiceBus"));

            await InitializeEndpointIfNecessary(functionContext, CancellationToken.None).ConfigureAwait(false);
        }

        static Dictionary<string, string> CreateNServiceBusHeaders(IDictionary<string, string> userProperties, string replyTo, string correlationId)
        {
            var d = new Dictionary<string, string>(userProperties);
            d.Remove("NServiceBus.Transport.Encoding");

            if (!string.IsNullOrWhiteSpace(replyTo))
            {
                d.TryAdd(Headers.ReplyToAddress, replyTo);
            }

            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                d.TryAdd(Headers.CorrelationId, correlationId);
            }

            return d;
        }

        readonly Func<FunctionContext, Task<IEndpointInstance>> endpointFactory;

        readonly SemaphoreSlim semaphoreLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);
        ServiceBusTriggeredEndpointConfiguration configuration;

        PipelineInvoker pipeline;
        IEndpointInstance endpoint;
    }
}