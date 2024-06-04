namespace NServiceBus.AzureFunctions.Worker.ServiceBus.Tests
{
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NUnit.Framework;

    public class When_outbox_is_enabled
    {
        [Test]
        public async Task Should_work()
        {
            var context = await Scenario.Define<Context>()
                .WithComponent(new OutboxEnabledFunction(new SomeMessage()))
                .Done(c => c.GotTheMessage)
                .Run();

            Assert.True(context.GotTheMessage);
        }

        class Context : ScenarioContext
        {
            public bool GotTheMessage { get; set; }
        }

        class OutboxEnabledFunction : FunctionEndpointComponent
        {
            public OutboxEnabledFunction(object triggerMessage)
            {
                CustomizeConfiguration = configuration =>
                {
                    configuration.AdvancedConfiguration.UsePersistence<AcceptanceTestingPersistence>();
                    configuration.AdvancedConfiguration.EnableOutbox();
                };
                AddTestMessage(triggerMessage);
            }

            class SomeMessageHandler(Context testContext) : IHandleMessages<SomeMessage>
            {
                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    testContext.GotTheMessage = true;
                    return Task.CompletedTask;
                }
            }
        }

        class SomeMessage : IMessage;
    }
}