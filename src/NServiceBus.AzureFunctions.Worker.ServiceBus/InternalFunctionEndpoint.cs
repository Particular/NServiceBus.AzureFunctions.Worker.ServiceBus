namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using AzureFunctions.Worker.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;

sealed class InternalFunctionEndpoint : IFunctionEndpoint
{
    internal InternalFunctionEndpoint(ServerlessTransport serverlessTransport, IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        this.serverlessTransport = serverlessTransport;
        this.serverlessTransport.ServiceProvider = serviceProvider;
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
                    messageSession = serviceProvider.GetRequiredService<IMessageSession>();

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
        await messageSession.Send(message, options, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task Send(object message, FunctionContext functionContext, CancellationToken cancellationToken = default)
        => Send(message, new SendOptions(), functionContext, cancellationToken);

    /// <inheritdoc />
    public async Task Send<T>(Action<T> messageConstructor, SendOptions options, FunctionContext functionContext, CancellationToken cancellationToken = default)
    {
        FunctionsLoggerFactory.Instance.SetCurrentLogger(functionContext.GetLogger("NServiceBus"));

        await InitializeEndpointIfNecessary(cancellationToken).ConfigureAwait(false);
        await messageSession.Send(messageConstructor, options, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task Send<T>(Action<T> messageConstructor, FunctionContext functionContext, CancellationToken cancellationToken = default)
        => Send(messageConstructor, new SendOptions(), functionContext, cancellationToken);

    /// <inheritdoc />
    public async Task Publish(object message, PublishOptions options, FunctionContext functionContext, CancellationToken cancellationToken = default)
    {
        FunctionsLoggerFactory.Instance.SetCurrentLogger(functionContext.GetLogger("NServiceBus"));

        await InitializeEndpointIfNecessary(cancellationToken).ConfigureAwait(false);
        await messageSession.Publish(message, options, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task Publish(object message, FunctionContext functionContext, CancellationToken cancellationToken = default)
        => Publish(message, new PublishOptions(), functionContext, cancellationToken);

    /// <inheritdoc />
    public async Task Publish<T>(Action<T> messageConstructor, PublishOptions options, FunctionContext functionContext, CancellationToken cancellationToken = default)
    {
        FunctionsLoggerFactory.Instance.SetCurrentLogger(functionContext.GetLogger("NServiceBus"));

        await InitializeEndpointIfNecessary(cancellationToken).ConfigureAwait(false);
        await messageSession.Publish(messageConstructor, options, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task Publish<T>(Action<T> messageConstructor, FunctionContext functionContext, CancellationToken cancellationToken = default)
        => Publish(messageConstructor, new PublishOptions(), functionContext, cancellationToken);

    /// <inheritdoc />
    public async Task Subscribe(Type eventType, SubscribeOptions options, FunctionContext functionContext, CancellationToken cancellationToken = default)
    {
        FunctionsLoggerFactory.Instance.SetCurrentLogger(functionContext.GetLogger("NServiceBus"));

        await InitializeEndpointIfNecessary(cancellationToken).ConfigureAwait(false);
        await messageSession.Subscribe(eventType, options, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task Subscribe(Type eventType, FunctionContext functionContext, CancellationToken cancellationToken = default)
        => Subscribe(eventType, new SubscribeOptions(), functionContext, cancellationToken);

    /// <inheritdoc />
    public async Task Unsubscribe(Type eventType, UnsubscribeOptions options, FunctionContext functionContext, CancellationToken cancellationToken = default)
    {
        FunctionsLoggerFactory.Instance.SetCurrentLogger(functionContext.GetLogger("NServiceBus"));

        await InitializeEndpointIfNecessary(cancellationToken).ConfigureAwait(false);
        await messageSession.Unsubscribe(eventType, options, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task Unsubscribe(Type eventType, FunctionContext functionContext, CancellationToken cancellationToken = default)
        => Unsubscribe(eventType, new UnsubscribeOptions(), functionContext, cancellationToken);

    readonly SemaphoreSlim semaphoreLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);
    readonly ServerlessTransport serverlessTransport;

    IMessageProcessor messageProcessor;
    IMessageSession messageSession;
    IServiceProvider serviceProvider;
}