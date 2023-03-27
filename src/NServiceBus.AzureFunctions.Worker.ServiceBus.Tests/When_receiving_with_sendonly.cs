namespace ServiceBus.Tests
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus;
    using NUnit.Framework;
    using System.Threading.Tasks;

    public class When_receiving_with_sendonly
    {
        [Test]
        public void Should_invoke_the_handler_to_process_it()
        {
            var exception = Assert.ThrowsAsync<InvalidOperationException>(() => Scenario.Define<ScenarioContext>()
                .WithComponent(new FunctionWithSendOnlyConfiguration())
                .Done(c => c.EndpointsStarted)
                .Run());

            StringAssert.Contains("This endpoint cannot process messages because it is configured in send-only mode.", exception.Message);
        }


        class FunctionWithSendOnlyConfiguration : FunctionEndpointComponent
        {
            public FunctionWithSendOnlyConfiguration()
            {
                CustomizeConfiguration = configuration => configuration.AdvancedConfiguration.SendOnly();

                TestMessages.Add(new TestMessage { Body = new ATestMessage(), UserProperties = new Dictionary<string, object>() });
            }

            public class TestMessageHandler : IHandleMessages<TestMessage>
            {
                public Task Handle(TestMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }
        }

        class ATestMessage : IMessage
        {
        }
    }
}