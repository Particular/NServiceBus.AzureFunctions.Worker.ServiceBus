﻿namespace NServiceBus.AzureFunctions.Worker.ServiceBus.Tests
{
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NUnit.Framework;

    public class When_publishing_event_from_function
    {
        [Test]
        public async Task Should_publish_to_subscribers()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<InsideSubscriber>()
                .WithComponent(new PublishingFunction())
                .Done(c => c.EventReceived)
                .Run();

            Assert.IsTrue(context.EventReceived);
        }

        class Context : ScenarioContext
        {
            public bool EventReceived { get; set; }
        }

        class InsideSubscriber : EndpointConfigurationBuilder
        {
            public InsideSubscriber() => EndpointSetup<DefaultEndpoint>();

            public class EventHandler(Context testContext) : IHandleMessages<InsideEvent>
            {
                public Task Handle(InsideEvent message, IMessageHandlerContext context)
                {
                    testContext.EventReceived = true;
                    return Task.CompletedTask;
                }
            }
        }

        class PublishingFunction : FunctionEndpointComponent
        {
            public PublishingFunction() => AddTestMessage(new TriggerMessage());

            public class PublishingHandler : IHandleMessages<TriggerMessage>
            {
                public Task Handle(TriggerMessage message, IMessageHandlerContext context) => context.Publish(new InsideEvent());
            }
        }

        class TriggerMessage : IMessage
        {
        }

        class InsideEvent : IEvent
        {
        }
    }
}