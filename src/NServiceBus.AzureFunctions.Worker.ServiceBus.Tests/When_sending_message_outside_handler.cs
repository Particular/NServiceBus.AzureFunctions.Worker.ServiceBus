namespace NServiceBus.AzureFunctions.Worker.ServiceBus.Tests
{
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NUnit.Framework;

    public class When_sending_message_outside_handler
    {
        [Test]
        public async Task Should_send_message_to_target_queue() =>
            await Scenario.Define<Context>()
                .WithEndpoint<ReceivingEndpoint>(b => b.When(async session =>
                {
                    var sendOptions = new SendOptions();
                    sendOptions.RouteToThisEndpoint();
                    await session.Send(new TriggerMessage(), sendOptions);
                }))
                .Done(c => c.HandlerReceivedMessage)
                .Run();

        class Context : ScenarioContext
        {
            public bool HandlerReceivedMessage { get; set; }
        }

        class ReceivingEndpoint : EndpointConfigurationBuilder
        {
            public ReceivingEndpoint() => EndpointSetup<DefaultEndpoint>();

            class TriggerMessageHandler(Context testContext) : IHandleMessages<TriggerMessage>
            {
                public Task Handle(TriggerMessage message, IMessageHandlerContext context)
                {
                    testContext.HandlerReceivedMessage = true;
                    return Task.CompletedTask;
                }
            }
        }

        class TriggerMessage : IMessage;
    }
}