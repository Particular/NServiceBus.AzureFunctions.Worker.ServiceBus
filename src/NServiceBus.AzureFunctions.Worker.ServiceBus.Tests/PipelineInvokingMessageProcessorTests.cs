namespace NServiceBus.AzureFunctions.Worker.ServiceBus.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Messaging.ServiceBus;
    using NServiceBus.AzureFunctions.Worker.ServiceBus;
    using NServiceBus.Transport;
    using NUnit.Framework;
    using Transport.AzureServiceBus;

    [TestFixture]
    public class PipelineInvokingMessageProcessorTests
    {
        static Task Process(object message, PipelineInvokingMessageProcessor pipeline)
        {
            var receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
                MessageHelper.GetBody(message), properties: MessageHelper.GetUserProperties(message),
                messageId: Guid.NewGuid().ToString("N"), deliveryCount: 1);
            return pipeline.Process(receivedMessage, new FakeServiceBusMessageActions());
        }

        [Test]
        public async Task When_processing_successful_should_complete_message()
        {
            MessageContext messageContext = null;
            var pipelineInvoker = await CreatePipeline(
                (ctx, __) =>
                {
                    messageContext = ctx;
                    return Task.CompletedTask;
                });

            var message = new TestMessage();
            var messageId = Guid.NewGuid().ToString("N");
            var body = MessageHelper.GetBody(message);
            var userProperties = MessageHelper.GetUserProperties(message);

            var serviceBusReceivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body, properties: userProperties,
                messageId: messageId, deliveryCount: 1);

            await pipelineInvoker.Process(serviceBusReceivedMessage, new FakeServiceBusMessageActions());

            Assert.Multiple(() =>
            {
                Assert.That(messageContext.Body.ToArray(), Is.EqualTo(body.ToArray()));
                Assert.That(messageContext.NativeMessageId, Is.SameAs(messageId));
            });
            CollectionAssert.IsSubsetOf(userProperties, messageContext.Headers); // the IncomingMessage has an additional MessageId header
            Assert.Multiple(() =>
            {
                Assert.That(messageContext.TransportTransaction.TryGet(out AzureServiceBusTransportTransaction transaction), Is.True);
                Assert.That(messageContext.TransportTransaction, Is.SameAs(transaction.TransportTransaction));
            });
        }

        [Test]
        public async Task When_processing_fails_should_provide_error_context()
        {
            var pipelineException = new Exception("test exception");
            MessageContext messageContext = null;
            ErrorContext errorContext = null;
            var pipelineInvoker = await CreatePipeline(
                (ctx, __) =>
                {
                    messageContext = ctx;
                    throw pipelineException;
                },
                (errCtx, _) =>
                {
                    errorContext = errCtx;
                    return Task.FromResult(ErrorHandleResult.Handled);
                });

            var message = new TestMessage();
            var messageId = Guid.NewGuid().ToString("N");
            var body = MessageHelper.GetBody(message);
            var userProperties = MessageHelper.GetUserProperties(message);

            var serviceBusReceivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body, properties: userProperties,
                messageId: messageId, deliveryCount: 1);

            await pipelineInvoker.Process(serviceBusReceivedMessage, new FakeServiceBusMessageActions());

            Assert.Multiple(() =>
            {
                Assert.That(errorContext.Exception, Is.SameAs(pipelineException));
                Assert.That(errorContext.Message.NativeMessageId, Is.SameAs(messageId));
                Assert.That(errorContext.Message.Body.ToArray(), Is.EqualTo(body.ToArray()));
            });
            CollectionAssert.IsSubsetOf(userProperties, errorContext.Message.Headers); // the IncomingMessage has an additional MessageId header
            Assert.Multiple(() =>
            {
                Assert.That(messageContext.TransportTransaction.TryGet(out AzureServiceBusTransportTransaction messageContextTransaction), Is.True);
                Assert.That(errorContext.TransportTransaction.TryGet(out AzureServiceBusTransportTransaction errorContextTransaction), Is.True);
                Assert.That(errorContext.TransportTransaction, Is.SameAs(errorContextTransaction.TransportTransaction)); // verify usage of the correct transport transaction instance
                Assert.That(errorContextTransaction.TransportTransaction, Is.Not.SameAs(messageContextTransaction)); // verify that a new transport transaction has been created for the error handling
            });
        }

        [Test]
        public async Task When_error_pipeline_fails_should_throw()
        {
            var errorPipelineException = new Exception("error pipeline failure");
            var pipelineInvoker = await CreatePipeline(
                (_, __) => throw new Exception("main pipeline failure"),
                (_, __) => throw errorPipelineException);

            var exception = Assert.ThrowsAsync<Exception>(async () =>
                await Process(new TestMessage(), pipelineInvoker));

            Assert.That(exception, Is.SameAs(errorPipelineException));
        }

        [Test]
        public async Task When_error_pipeline_handles_error_should_not_throw()
        {
            var pipelineInvoker = await CreatePipeline(
                (_, __) => throw new Exception("main pipeline failure"),
                (_, __) => Task.FromResult(ErrorHandleResult.Handled));

            Assert.DoesNotThrowAsync(async () => await Process(new TestMessage(), pipelineInvoker));
        }

        [Test]
        public async Task When_error_pipeline_requires_retry_should_throw()
        {
            var mainPipelineException = new Exception("main pipeline failure");
            var pipelineInvoker = await CreatePipeline(
                (_, __) => throw mainPipelineException,
                (_, __) => Task.FromResult(ErrorHandleResult.RetryRequired));

            var exception = Assert.ThrowsAsync<Exception>(async () =>
                await Process(new TestMessage(), pipelineInvoker));

            Assert.That(exception, Is.SameAs(mainPipelineException));
        }

        static async Task<PipelineInvokingMessageProcessor> CreatePipeline(OnMessage mainPipeline = null, OnError errorPipeline = null)
        {
            var pipelineInvoker = new PipelineInvokingMessageProcessor(new FakeMessageReceiver());
            await pipelineInvoker.Initialize(null,
                mainPipeline ?? ((_, __) => Task.CompletedTask),
                errorPipeline ?? ((_, __) => Task.FromResult(ErrorHandleResult.Handled)),
                CancellationToken.None);
            return pipelineInvoker;
        }

        class FakeMessageReceiver : IMessageReceiver
        {
            public ISubscriptionManager Subscriptions => throw new NotImplementedException();

            public string Id => "FakeId";

            public string ReceiveAddress => "FakeAddress";

            public Task Initialize(PushRuntimeSettings limitations, OnMessage onMessage, OnError onError, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task StartReceive(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task ChangeConcurrency(PushRuntimeSettings limitations, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task StopReceive(CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        class TestMessage;
    }
}