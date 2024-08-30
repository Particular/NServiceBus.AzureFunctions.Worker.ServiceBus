namespace NServiceBus.AzureFunctions.Worker.ServiceBus.Tests
{
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Messaging.ServiceBus;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NUnit.Framework;

    public class When_function_receives_a_message
    {
        [Test]
        public async Task Should_invoke_the_handler_to_process_it()
        {
            var context = await Scenario.Define<Context>()
                .WithComponent(new FunctionHandler(new HappyDayMessage()))
                .Done(c => c.HandlerInvocationCount > 0)
                .Run();

            Assert.Multiple(() =>
            {
                Assert.That(context.HandlerInvocationCount, Is.EqualTo(1));
                Assert.That(context.ReceivedMessageAvailable, Is.True);
            });
        }

        class Context : ScenarioContext
        {
            public int HandlerInvocationCount => count;
            public bool ReceivedMessageAvailable { get; set; }

            public void HandlerInvoked() => Interlocked.Increment(ref count);

            int count;
        }

        class FunctionHandler : FunctionEndpointComponent
        {
            public FunctionHandler(object triggerMessage) => AddTestMessage(triggerMessage);

            class HappyDayMessageHandler(Context testContext) : IHandleMessages<HappyDayMessage>
            {
                public Task Handle(HappyDayMessage message, IMessageHandlerContext context)
                {
                    testContext.HandlerInvoked();
                    testContext.ReceivedMessageAvailable = context.Extensions.TryGet(out ServiceBusReceivedMessage _);
                    return Task.CompletedTask;
                }
            }
        }

        class HappyDayMessage : IMessage;
    }
}