namespace NServiceBus.AzureFunctions.Worker.ServiceBus.Tests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NUnit.Framework;

    public class When_receiving_with_sendonly
    {
        [Test]
        public void Should_invoke_the_handler_to_process_it()
        {
            var exception = Assert.ThrowsAsync<InvalidOperationException>(() => Scenario.Define<ScenarioContext>()
                .WithComponent(new FunctionWithSendOnlyConfiguration())
                .Done(c => c.EndpointsStarted)
                .Run());

            Assert.That(exception.Message, Does.Contain("This endpoint cannot process messages because it is configured in send-only mode."));
        }


        class FunctionWithSendOnlyConfiguration : FunctionEndpointComponent
        {
            public FunctionWithSendOnlyConfiguration()
            {
                CustomizeConfiguration = configuration => configuration.AdvancedConfiguration.SendOnly();

                AddTestMessage(new ATestMessage());
            }

            class TestMessageHandler : IHandleMessages<ATestMessage>
            {
                public Task Handle(ATestMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }
        }

        class ATestMessage : IMessage;
    }
}