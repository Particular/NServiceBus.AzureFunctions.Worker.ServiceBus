#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;

[ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7", ReplacementTypeOrMember = nameof(IFunctionEndpoint))]
public class FunctionEndpoint : IFunctionEndpoint
{
    public Task Process(ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions,
        FunctionContext functionContext, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task Send(object message, SendOptions options, FunctionContext functionContext,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task Send(object message, FunctionContext functionContext, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public Task Send<T>(Action<T> messageConstructor, SendOptions options, FunctionContext functionContext,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task Send<T>(Action<T> messageConstructor, FunctionContext functionContext, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public Task Publish(object message, PublishOptions options, FunctionContext functionContext,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task Publish<T>(Action<T> messageConstructor, PublishOptions options, FunctionContext functionContext,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task Publish(object message, FunctionContext functionContext, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public Task Publish<T>(Action<T> messageConstructor, FunctionContext functionContext,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task Subscribe(Type eventType, SubscribeOptions options, FunctionContext functionContext,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task Subscribe(Type eventType, FunctionContext functionContext, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public Task Unsubscribe(Type eventType, UnsubscribeOptions options, FunctionContext functionContext,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task Unsubscribe(Type eventType, FunctionContext functionContext, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member