namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using AzureFunctions.Worker.ServiceBus;
using Microsoft.Azure.Functions.Worker;

sealed class InternalFunctionEndpoint : IFunctionEndpoint
{
    internal InternalFunctionEndpoint(IStartableEndpointWithExternallyManagedContainer externallyManagedContainerEndpoint, ServerlessTransport serverlessTransport, IServiceProvider serviceProvider)
    {
        this.serverlessTransport = serverlessTransport;
        this.serverlessTransport.ServiceProvider = serviceProvider;
        endpointFactory = () => externallyManagedContainerEndpoint.Start(serviceProvider);
    }

    /// <inheritdoc />
    public async Task Process(ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions, FunctionContext functionContext,
        CancellationToken cancellationToken = default)
    {
        FunctionsLoggerFactory.Instance.SetCurrentLogger(functionContext.GetLogger("NServiceBus"));

        await InitializeEndpointIfNecessary(cancellationToken)
            .ConfigureAwait(false);

        await messageProcessor.Process(message, messageActions, cancellationToken)
            .ConfigureAwait(false);
    }

    internal async Task InitializeEndpointIfNecessary(CancellationToken cancellationToken = default)
    {
        if (messageProcessor == null)
        {
            await semaphoreLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (messageProcessor == null)
                {
                    endpoint = await endpointFactory().ConfigureAwait(false);

                    messageProcessor = serverlessTransport.MessageProcessor;
                }
            }
            finally
            {
                semaphoreLock.Release();
            }
        }
    }

    /// <inheritdoc />
    public async Task Send(object message, SendOptions options, FunctionContext functionContext, CancellationToken cancellationToken = default)
    {
        FunctionsLoggerFactory.Instance.SetCurrentLogger(functionContext.GetLogger("NServiceBus"));

        await InitializeEndpointIfNecessary(cancellationToken).ConfigureAwait(false);
        await endpoint.Send(message, options, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task Send(object message, FunctionContext functionContext, CancellationToken cancellationToken = default)
        => Send(message, new SendOptions(), functionContext, cancellationToken);

    /// <inheritdoc />
    public async Task Send<T>(Action<T> messageConstructor, SendOptions options, FunctionContext functionContext, CancellationToken cancellationToken = default)
    {
        FunctionsLoggerFactory.Instance.SetCurrentLogger(functionContext.GetLogger("NServiceBus"));

        await InitializeEndpointIfNecessary(cancellationToken).ConfigureAwait(false);
        await endpoint.Send(messageConstructor, options, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task Send<T>(Action<T> messageConstructor, FunctionContext functionContext, CancellationToken cancellationToken = default)
        => Send(messageConstructor, new SendOptions(), functionContext, cancellationToken);

    /// <inheritdoc />
    public async Task Publish(object message, PublishOptions options, FunctionContext functionContext, CancellationToken cancellationToken = default)
    {
        FunctionsLoggerFactory.Instance.SetCurrentLogger(functionContext.GetLogger("NServiceBus"));

        await InitializeEndpointIfNecessary(cancellationToken).ConfigureAwait(false);
        await endpoint.Publish(message, options, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task Publish(object message, FunctionContext functionContext, CancellationToken cancellationToken = default)
        => Publish(message, new PublishOptions(), functionContext, cancellationToken);

    /// <inheritdoc />
    public async Task Publish<T>(Action<T> messageConstructor, PublishOptions options, FunctionContext functionContext, CancellationToken cancellationToken = default)
    {
        FunctionsLoggerFactory.Instance.SetCurrentLogger(functionContext.GetLogger("NServiceBus"));

        await InitializeEndpointIfNecessary(cancellationToken).ConfigureAwait(false);
        await endpoint.Publish(messageConstructor, options, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task Publish<T>(Action<T> messageConstructor, FunctionContext functionContext, CancellationToken cancellationToken = default)
        => Publish(messageConstructor, new PublishOptions(), functionContext, cancellationToken);

    /// <inheritdoc />
    public async Task Subscribe(Type eventType, SubscribeOptions options, FunctionContext functionContext, CancellationToken cancellationToken = default)
    {
        FunctionsLoggerFactory.Instance.SetCurrentLogger(functionContext.GetLogger("NServiceBus"));

        await InitializeEndpointIfNecessary(cancellationToken).ConfigureAwait(false);
        await endpoint.Subscribe(eventType, options, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task Subscribe(Type eventType, FunctionContext functionContext, CancellationToken cancellationToken = default)
        => Subscribe(eventType, new SubscribeOptions(), functionContext, cancellationToken);

    /// <inheritdoc />
    public async Task Unsubscribe(Type eventType, UnsubscribeOptions options, FunctionContext functionContext, CancellationToken cancellationToken = default)
    {
        FunctionsLoggerFactory.Instance.SetCurrentLogger(functionContext.GetLogger("NServiceBus"));

        await InitializeEndpointIfNecessary(cancellationToken).ConfigureAwait(false);
        await endpoint.Unsubscribe(eventType, options, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task Unsubscribe(Type eventType, FunctionContext functionContext, CancellationToken cancellationToken = default)
        => Unsubscribe(eventType, new UnsubscribeOptions(), functionContext, cancellationToken);

    readonly Func<Task<IEndpointInstance>> endpointFactory;

    readonly SemaphoreSlim semaphoreLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);
    readonly ServerlessTransport serverlessTransport;

    IMessageProcessor messageProcessor;
    IEndpointInstance endpoint;
}