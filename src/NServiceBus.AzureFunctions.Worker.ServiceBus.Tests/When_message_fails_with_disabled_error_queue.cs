namespace NServiceBus.AzureFunctions.Worker.ServiceBus.Tests;

using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;

public class When_message_fails_with_disabled_error_queue
{
    [Test]
    public void Should_throw_exception()
    {
        var exception = Assert.ThrowsAsync<Exception>(() =>
        {
            return Scenario.Define<ScenarioContext>()
                .WithComponent(new FailingFunction())
                .Done(c => c.EndpointsStarted)
                .Run();
        });

        Assert.That(exception.Message, Does.Contain("Failed to process message"));
        Assert.That(exception.InnerException, Is.InstanceOf<SimulatedException>());
    }

    class FailingFunction : FunctionEndpointComponent
    {
        public FailingFunction()
        {
            CustomizeConfiguration = c => c.DoNotSendMessagesToErrorQueue();

            AddTestMessage(new TriggerMessage());
        }

        class FailingHandler : IHandleMessages<TriggerMessage>
        {
            public Task Handle(TriggerMessage message, IMessageHandlerContext context) => throw new SimulatedException();
        }
    }

    class TriggerMessage : IMessage;
}