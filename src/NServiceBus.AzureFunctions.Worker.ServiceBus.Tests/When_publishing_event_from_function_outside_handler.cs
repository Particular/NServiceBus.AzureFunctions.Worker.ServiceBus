namespace NServiceBus.AzureFunctions.Worker.ServiceBus.Tests
{
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NUnit.Framework;

    public class When_publishing_event_from_function_outside_handler
    {
        [Test]
        public async Task Should_publish_to_subscribers()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<OutsideSubscriber>(b =>
                    b.When(async session => await session.Publish(new OutsideEvent())))
                .Done(c => c.EventReceived)
                .Run();

            Assert.That(context.EventReceived, Is.True);
        }

        class Context : ScenarioContext
        {
            public bool EventReceived { get; set; }
        }

        class OutsideSubscriber : EndpointConfigurationBuilder
        {
            public OutsideSubscriber() => EndpointSetup<DefaultEndpoint>();

            class EventHandler(Context testContext) : IHandleMessages<OutsideEvent>
            {
                public Task Handle(OutsideEvent message, IMessageHandlerContext context)
                {
                    testContext.EventReceived = true;
                    return Task.CompletedTask;
                }
            }
        }

        class OutsideEvent : IEvent;
    }
}