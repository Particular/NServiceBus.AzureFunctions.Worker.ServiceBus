namespace ServiceBus.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus;
    using NServiceBus.AzureFunctions.Worker.ServiceBus;
    using NServiceBus.Transport;
    using NUnit.Framework;

    [TestFixture]
    public class FunctionEndpointTests
    {
        static Task Process(object message, ITransactionStrategy transactionStrategy, PipelineInvoker pipeline)
        {
            return FunctionEndpoint.Process(
                MessageHelper.GetBody(message),
                Guid.NewGuid().ToString("N"),
                1,
                null,
                null,
                transactionStrategy,
                pipeline,
                new FakeFunctionContext(MessageHelper.GetUserProperties(message)),
                CancellationToken.None);
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

            var transactionStrategy = new TestableFunctionTransactionStrategy();

            var message = new TestMessage();
            var messageId = Guid.NewGuid().ToString("N");
            var body = MessageHelper.GetBody(message);
            var userProperties = MessageHelper.GetUserProperties(message);
            await FunctionEndpoint.Process(
                body,
                messageId,
                1,
                null,
                null,
                transactionStrategy,
                pipelineInvoker,
                new FakeFunctionContext(userProperties),
                CancellationToken.None);

            Assert.IsTrue(transactionStrategy.OnCompleteCalled);
            Assert.AreEqual(body, messageContext.Body.ToArray());
            Assert.AreSame(messageId, messageContext.NativeMessageId);
            CollectionAssert.IsSubsetOf(userProperties, messageContext.Headers); // the IncomingMessage has an additional MessageId header
            Assert.AreEqual(1, transactionStrategy.CreatedTransportTransactions.Count);
            Assert.AreSame(transactionStrategy.CreatedTransportTransactions[0], messageContext.TransportTransaction);
        }

        [Test]
        public async Task When_processing_fails_should_provide_error_context()
        {
            var pipelineException = new Exception("test exception");
            ErrorContext errorContext = null;
            var pipelineInvoker = await CreatePipeline(
                (_, __) => throw pipelineException,
                (errCtx, _) =>
                {
                    errorContext = errCtx;
                    return Task.FromResult(ErrorHandleResult.Handled);
                });

            var transactionStrategy = new TestableFunctionTransactionStrategy();

            var message = new TestMessage();
            var messageId = Guid.NewGuid().ToString("N");
            var body = MessageHelper.GetBody(message);
            var userProperties = MessageHelper.GetUserProperties(message);
            await FunctionEndpoint.Process(
                body,
                messageId,
                1,
                null,
                null,
                transactionStrategy,
                pipelineInvoker,
                new FakeFunctionContext(userProperties),
                CancellationToken.None);

            Assert.AreSame(pipelineException, errorContext.Exception);
            Assert.AreSame(messageId, errorContext.Message.NativeMessageId);
            Assert.AreEqual(body, errorContext.Message.Body.ToArray());
            CollectionAssert.IsSubsetOf(userProperties, errorContext.Message.Headers); // the IncomingMessage has an additional MessageId header
            Assert.AreSame(transactionStrategy.CreatedTransportTransactions.Last(), errorContext.TransportTransaction); // verify usage of the correct transport transaction instance
            Assert.AreEqual(2, transactionStrategy.CreatedTransportTransactions.Count); // verify that a new transport transaction has been created for the error handling
        }

        [Test]
        public async Task When_error_pipeline_fails_should_throw()
        {
            var errorPipelineException = new Exception("error pipeline failure");
            var pipelineInvoker = await CreatePipeline(
                (_, __) => throw new Exception("main pipeline failure"),
                (_, __) => throw errorPipelineException);

            var transactionStrategy = new TestableFunctionTransactionStrategy();

            var exception = Assert.ThrowsAsync<Exception>(async () =>
                await Process(new TestMessage(), transactionStrategy, pipelineInvoker));

            Assert.IsFalse(transactionStrategy.OnCompleteCalled);
            Assert.AreSame(errorPipelineException, exception);
        }

        [Test]
        public async Task When_error_pipeline_handles_error_should_complete_message()
        {
            var pipelineInvoker = await CreatePipeline(
                (_, __) => throw new Exception("main pipeline failure"),
                (_, __) => Task.FromResult(ErrorHandleResult.Handled));

            var transactionStrategy = new TestableFunctionTransactionStrategy();

            await Process(new TestMessage(), transactionStrategy, pipelineInvoker);

            Assert.IsTrue(transactionStrategy.OnCompleteCalled);
        }

        [Test]
        public async Task When_error_pipeline_requires_retry_should_throw()
        {
            var mainPipelineException = new Exception("main pipeline failure");
            var pipelineInvoker = await CreatePipeline(
                (_, __) => throw mainPipelineException,
                (_, __) => Task.FromResult(ErrorHandleResult.RetryRequired));

            var transactionStrategy = new TestableFunctionTransactionStrategy();

            var exception = Assert.ThrowsAsync<Exception>(async () =>
                await Process(new TestMessage(), transactionStrategy, pipelineInvoker));

            Assert.IsFalse(transactionStrategy.OnCompleteCalled);
            Assert.AreSame(mainPipelineException, exception);
        }

        static async Task<PipelineInvoker> CreatePipeline(OnMessage mainPipeline = null, OnError errorPipeline = null)
        {
            var pipelineInvoker = new PipelineInvoker(null);
            await pipelineInvoker.Initialize(null,
                mainPipeline ?? ((_, __) => Task.CompletedTask),
                errorPipeline ?? ((_, __) => Task.FromResult(ErrorHandleResult.Handled)),
                default);
            return pipelineInvoker;
        }

        class TestMessage
        {
        }

        class TestableFunctionTransactionStrategy : ITransactionStrategy
        {
            public bool OnCompleteCalled { get; private set; }
            public CommittableTransaction CompletedTransaction { get; private set; }
            public CommittableTransaction CreatedTransaction { get; private set; }
            public List<TransportTransaction> CreatedTransportTransactions { get; } = new List<TransportTransaction>();

            public Task Complete(CommittableTransaction transaction)
            {
                OnCompleteCalled = true;
                CompletedTransaction = transaction;
                return Task.CompletedTask;
            }

            public CommittableTransaction CreateTransaction()
            {
                CreatedTransaction = new CommittableTransaction();
                return CreatedTransaction;
            }

            public TransportTransaction CreateTransportTransaction(CommittableTransaction transaction)
            {
                var transportTransaction = new TransportTransaction();
                CreatedTransportTransactions.Add(transportTransaction);
                return transportTransaction;
            }
        }
    }
}