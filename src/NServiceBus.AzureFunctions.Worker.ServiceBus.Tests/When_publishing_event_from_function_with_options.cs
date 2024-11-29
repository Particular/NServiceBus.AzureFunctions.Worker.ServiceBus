namespace NServiceBus.AzureFunctions.Worker.ServiceBus.Tests
{
    using System.Threading.Tasks;
    using Microsoft.Azure.Functions.Worker;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NUnit.Framework;

    public class When_publishing_event_from_function_with_options
    {
        [Test]
        public async Task Should_publish_to_subscribers_with_headers()
        {
            var options = new PublishOptions();
            options.SetHeader("TestKey", "TestValue");

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Subscriber>()
                .WithComponent(new TestFunction())
                .Done(c => c.EventReceived)
                .Run();

            Assert.That(context.EventReceived, Is.True);
            Assert.That(context.CustomHeaderReceived, Is.True, "TestKey header not on received message");
        }

        class Context : ScenarioContext
        {
            public bool EventReceived { get; set; }
            public bool CustomHeaderReceived { get; set; }
        }

        class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber() => EndpointSetup<DefaultEndpoint>();

            class EventHandler(Context testContext) : IHandleMessages<EventWithOptions>
            {
                public Task Handle(EventWithOptions message, IMessageHandlerContext context)
                {
                    testContext.CustomHeaderReceived = context.MessageHeaders.ContainsKey("TestKey");
                    testContext.EventReceived = true;
                    return Task.CompletedTask;
                }
            }
        }

        class TestFunction : FunctionEndpointComponent
        {
            protected override Task OnStart(IFunctionEndpoint endpoint, FunctionContext executionContext)
            {
                var options = new PublishOptions();
                options.SetHeader("TestKey", "TestValue");
                return endpoint.Publish(new EventWithOptions(), options, executionContext);
            }
        }

        class EventWithOptions : IEvent;
    }
}