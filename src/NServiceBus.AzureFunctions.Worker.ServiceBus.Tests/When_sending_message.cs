namespace NServiceBus.AzureFunctions.Worker.ServiceBus.Tests;

using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;
using Conventions = AcceptanceTesting.Customization.Conventions;

public class When_sending_message
{
    [Test]
    public async Task Should_send_message_to_target_queue() =>
        await Scenario.Define<Context>()
            .WithEndpoint<ReceivingEndpoint>()
            .WithComponent(new SendingFunction(new TriggerMessage()))
            .Done(c => c.HandlerReceivedMessage)
            .Run();

    class Context : ScenarioContext
    {
        public bool HandlerReceivedMessage { get; set; }
    }

    class ReceivingEndpoint : EndpointConfigurationBuilder
    {
        public ReceivingEndpoint() => EndpointSetup<DefaultEndpoint>();

        class OutgoingMessageHandler(Context testContext) : IHandleMessages<FollowupMessage>
        {
            public Task Handle(FollowupMessage message, IMessageHandlerContext context)
            {
                testContext.HandlerReceivedMessage = true;
                return Task.CompletedTask;
            }
        }
    }

    class SendingFunction : FunctionEndpointComponent
    {
        public SendingFunction(object triggerMessage) => AddTestMessage(triggerMessage);

        class TriggerMessageHandler : IHandleMessages<TriggerMessage>
        {
            public Task Handle(TriggerMessage message, IMessageHandlerContext context) => context.Send(Conventions.EndpointNamingConvention(typeof(ReceivingEndpoint)), new FollowupMessage());
        }
    }

    class TriggerMessage : IMessage;

    class FollowupMessage : IMessage;
}