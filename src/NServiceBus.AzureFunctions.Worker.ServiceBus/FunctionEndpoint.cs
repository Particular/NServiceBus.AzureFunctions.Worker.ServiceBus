namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AzureFunctions.Worker.ServiceBus;
    using Extensibility;
    using Microsoft.Azure.Functions.Worker;
    using Transport;
    using Transport.AzureServiceBus;

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
            IDictionary<string, object> userProperties,
            string messageId,
            int deliveryCount,
            string replyTo,
            string correlationId,
            FunctionContext functionContext,
            CancellationToken cancellationToken)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionContext.GetLogger("NServiceBus"));

            await InitializeEndpointIfNecessary(functionContext, CancellationToken.None)
                .ConfigureAwait(false);

            await Process(body, userProperties, messageId, deliveryCount, replyTo, correlationId, pipeline, cancellationToken)
                .ConfigureAwait(false);
        }

        internal static async Task Process(
            byte[] body,
            IDictionary<string, object> userProperties,
            string messageId,
            int deliveryCount,
            string replyTo,
            string correlationId,
            PipelineInvoker pipeline,
            CancellationToken cancellationToken)
        {
            body ??= Array.Empty<byte>(); // might be null
            messageId ??= Guid.NewGuid().ToString("N");

            try
            {
                using var azureServiceBusTransaction = new AzureServiceBusTransportTransaction();
                var messageContext = new MessageContext(
                    messageId,
                    CreateNServiceBusHeaders(userProperties, replyTo, correlationId),
                    body,
                    azureServiceBusTransaction.TransportTransaction,
                    pipeline.ReceiveAddress,
                    new ContextBag());

                await pipeline.PushMessage(messageContext, cancellationToken).ConfigureAwait(false);

                azureServiceBusTransaction.Commit();
            }
            catch (Exception exception)
            {
                using var azureServiceBusTransaction = new AzureServiceBusTransportTransaction();
                var errorContext = new ErrorContext(
                    exception,
                    CreateNServiceBusHeaders(userProperties, replyTo, correlationId),
                    messageId,
                    body,
                    azureServiceBusTransaction.TransportTransaction,
                    deliveryCount,
                    pipeline.ReceiveAddress,
                    new ContextBag());

                ErrorHandleResult errorHandleResult = await pipeline.PushFailedMessage(errorContext, cancellationToken)
                    .ConfigureAwait(false);

                azureServiceBusTransaction.Commit();

                if (errorHandleResult == ErrorHandleResult.Handled)
                {
                    return;
                }

                throw;
            }
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
        public async Task Send(object message, SendOptions options, FunctionContext functionContext, CancellationToken cancellationToken)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionContext.GetLogger("NServiceBus"));

            await InitializeEndpointIfNecessary(functionContext, cancellationToken).ConfigureAwait(false);
            await endpoint.Send(message, options, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task Send(object message, FunctionContext functionContext, CancellationToken cancellationToken)
            => Send(message, new SendOptions(), functionContext, cancellationToken);

        /// <inheritdoc />
        public async Task Send<T>(Action<T> messageConstructor, SendOptions options, FunctionContext functionContext, CancellationToken cancellationToken)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionContext.GetLogger("NServiceBus"));

            await InitializeEndpointIfNecessary(functionContext, cancellationToken).ConfigureAwait(false);
            await endpoint.Send(messageConstructor, options, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task Send<T>(Action<T> messageConstructor, FunctionContext functionContext, CancellationToken cancellationToken)
            => Send(messageConstructor, new SendOptions(), functionContext, cancellationToken);

        /// <inheritdoc />
        public async Task Publish(object message, PublishOptions options, FunctionContext functionContext, CancellationToken cancellationToken)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionContext.GetLogger("NServiceBus"));

            await InitializeEndpointIfNecessary(functionContext, cancellationToken).ConfigureAwait(false);
            await endpoint.Publish(message, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task Publish(object message, FunctionContext functionContext, CancellationToken cancellationToken)
            => Publish(message, new PublishOptions(), functionContext, cancellationToken);

        /// <inheritdoc />
        public async Task Publish<T>(Action<T> messageConstructor, PublishOptions options, FunctionContext functionContext, CancellationToken cancellationToken)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionContext.GetLogger("NServiceBus"));

            await InitializeEndpointIfNecessary(functionContext, cancellationToken).ConfigureAwait(false);
            await endpoint.Publish(messageConstructor, options, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task Publish<T>(Action<T> messageConstructor, FunctionContext functionContext, CancellationToken cancellationToken)
            => Publish(messageConstructor, new PublishOptions(), functionContext, cancellationToken);

        /// <inheritdoc />
        public async Task Subscribe(Type eventType, SubscribeOptions options, FunctionContext functionContext, CancellationToken cancellationToken)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionContext.GetLogger("NServiceBus"));

            await InitializeEndpointIfNecessary(functionContext, cancellationToken).ConfigureAwait(false);
            await endpoint.Subscribe(eventType, options, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task Subscribe(Type eventType, FunctionContext functionContext, CancellationToken cancellationToken)
            => Subscribe(eventType, new SubscribeOptions(), functionContext, cancellationToken);

        /// <inheritdoc />
        public async Task Unsubscribe(Type eventType, UnsubscribeOptions options, FunctionContext functionContext, CancellationToken cancellationToken)
        {
            FunctionsLoggerFactory.Instance.SetCurrentLogger(functionContext.GetLogger("NServiceBus"));

            await InitializeEndpointIfNecessary(functionContext, cancellationToken).ConfigureAwait(false);
            await endpoint.Unsubscribe(eventType, options, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task Unsubscribe(Type eventType, FunctionContext functionContext, CancellationToken cancellationToken)
            => Unsubscribe(eventType, new UnsubscribeOptions(), functionContext, cancellationToken);

        static Dictionary<string, string> CreateNServiceBusHeaders(IDictionary<string, object> userProperties, string replyTo, string correlationId)
        {
            var headers = new Dictionary<string, string>(userProperties.Count);

            foreach (var userProperty in userProperties)
            {
                headers[userProperty.Key] = userProperty.Value.ToString();
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

        readonly Func<FunctionContext, Task<IEndpointInstance>> endpointFactory;

        readonly SemaphoreSlim semaphoreLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);
        ServiceBusTriggeredEndpointConfiguration configuration;

        PipelineInvoker pipeline;
        IEndpointInstance endpoint;
    }
}