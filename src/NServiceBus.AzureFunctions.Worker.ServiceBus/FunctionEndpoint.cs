namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AzureFunctions.Worker.ServiceBus;
    using Microsoft.Azure.Functions.Worker;

    /// <summary>
    /// An NServiceBus endpoint hosted in Azure Function which does not receive messages automatically but only handles
    /// messages explicitly passed to it by the caller.
    /// </summary>
    public class FunctionEndpoint : IFunctionEndpoint
    {
        internal FunctionEndpoint(IStartableEndpointWithExternallyManagedContainer externallyManagedContainerEndpoint, ServerlessInterceptor serverless, IServiceProvider serviceProvider)
        {
            this.serverless = serverless;
            endpointFactory = () => externallyManagedContainerEndpoint.Start(serviceProvider);
        }

        /// <inheritdoc />
        public async Task Process(
            byte[] body,
            IDictionary<string, object> userProperties,
            string messageId,
            int deliveryCount,
            string replyTo,
            string correlationId,
            FunctionContext functionContext,
            CancellationToken cancellationToken)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionContext.GetLogger("NServiceBus"));

            await InitializeEndpointIfNecessary(CancellationToken.None)
                .ConfigureAwait(false);

            await messageProcessor.Process(body, userProperties, messageId, deliveryCount, replyTo, correlationId, NoTransactionStrategy.Instance, cancellationToken)
                .ConfigureAwait(false);
        }

        internal async Task InitializeEndpointIfNecessary(CancellationToken cancellationToken)
        {
            if (messageProcessor == null)
            {
                await semaphoreLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    if (messageProcessor == null)
                    {
                        endpoint = await endpointFactory().ConfigureAwait(false);

                        messageProcessor = serverless.MessageProcessor;
                    }
                }
                finally
                {
                    semaphoreLock.Release();
                }
            }
        }

        /// <inheritdoc />
        public async Task Send(object message, SendOptions options, FunctionContext functionContext, CancellationToken cancellationToken)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionContext.GetLogger("NServiceBus"));

            await InitializeEndpointIfNecessary(cancellationToken).ConfigureAwait(false);
            await endpoint.Send(message, options, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task Send(object message, FunctionContext functionContext, CancellationToken cancellationToken)
            => Send(message, new SendOptions(), functionContext, cancellationToken);

        /// <inheritdoc />
        public async Task Send<T>(Action<T> messageConstructor, SendOptions options, FunctionContext functionContext, CancellationToken cancellationToken)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionContext.GetLogger("NServiceBus"));

            await InitializeEndpointIfNecessary(cancellationToken).ConfigureAwait(false);
            await endpoint.Send(messageConstructor, options, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task Send<T>(Action<T> messageConstructor, FunctionContext functionContext, CancellationToken cancellationToken)
            => Send(messageConstructor, new SendOptions(), functionContext, cancellationToken);

        /// <inheritdoc />
        public async Task Publish(object message, PublishOptions options, FunctionContext functionContext, CancellationToken cancellationToken)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionContext.GetLogger("NServiceBus"));

            await InitializeEndpointIfNecessary(cancellationToken).ConfigureAwait(false);
            await endpoint.Publish(message, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task Publish(object message, FunctionContext functionContext, CancellationToken cancellationToken)
            => Publish(message, new PublishOptions(), functionContext, cancellationToken);

        /// <inheritdoc />
        public async Task Publish<T>(Action<T> messageConstructor, PublishOptions options, FunctionContext functionContext, CancellationToken cancellationToken)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionContext.GetLogger("NServiceBus"));

            await InitializeEndpointIfNecessary(cancellationToken).ConfigureAwait(false);
            await endpoint.Publish(messageConstructor, options, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task Publish<T>(Action<T> messageConstructor, FunctionContext functionContext, CancellationToken cancellationToken)
            => Publish(messageConstructor, new PublishOptions(), functionContext, cancellationToken);

        /// <inheritdoc />
        public async Task Subscribe(Type eventType, SubscribeOptions options, FunctionContext functionContext, CancellationToken cancellationToken)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionContext.GetLogger("NServiceBus"));

            await InitializeEndpointIfNecessary(cancellationToken).ConfigureAwait(false);
            await endpoint.Subscribe(eventType, options, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task Subscribe(Type eventType, FunctionContext functionContext, CancellationToken cancellationToken)
            => Subscribe(eventType, new SubscribeOptions(), functionContext, cancellationToken);

        /// <inheritdoc />
        public async Task Unsubscribe(Type eventType, UnsubscribeOptions options, FunctionContext functionContext, CancellationToken cancellationToken)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionContext.GetLogger("NServiceBus"));

            await InitializeEndpointIfNecessary(cancellationToken).ConfigureAwait(false);
            await endpoint.Unsubscribe(eventType, options, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task Unsubscribe(Type eventType, FunctionContext functionContext, CancellationToken cancellationToken)
            => Unsubscribe(eventType, new UnsubscribeOptions(), functionContext, cancellationToken);

        readonly Func<Task<IEndpointInstance>> endpointFactory;

        readonly SemaphoreSlim semaphoreLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);
        readonly ServerlessInterceptor serverless;

        IMessageProcessor messageProcessor;
        IEndpointInstance endpoint;
    }
}