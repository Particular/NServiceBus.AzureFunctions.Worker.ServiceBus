namespace NServiceBus.AzureFunctions.Worker.ServiceBus.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Azure.Messaging.ServiceBus;
    using NUnit.Framework;

    public class When_using_token_credential_with_fully_qualified_namespace
    {
        string fullyQualifiedNamespace;

        string originalConnectionString;

        [SetUp]
        public void SetUp()
        {
            string defaultConnectionStringKey = ServerlessTransport.DefaultServiceBusConnectionName;
            originalConnectionString = Environment.GetEnvironmentVariable(defaultConnectionStringKey);

            var connectionStringProperties = ServiceBusConnectionStringProperties.Parse(originalConnectionString);
            fullyQualifiedNamespace = connectionStringProperties.FullyQualifiedNamespace;

            Environment.SetEnvironmentVariable(defaultConnectionStringKey, null, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable($"{defaultConnectionStringKey}__fullyQualifiedNamespace",
                fullyQualifiedNamespace, EnvironmentVariableTarget.Process);
        }

        [Test]
        public async Task Should_work()
        {
            Context context = await Scenario.Define<Context>()
                .WithComponent(new FunctionUsingTokenAuth())
                .Done(c => c.HandlerInvocationCount > 0)
                .Run();

            Assert.That(context.HandlerInvocationCount, Is.EqualTo(1));
        }

        [TearDown]
        public void TearDown()
        {
            string defaultConnectionStringKey = ServerlessTransport.DefaultServiceBusConnectionName;
            Environment.SetEnvironmentVariable($"{defaultConnectionStringKey}__fullyQualifiedNamespace",
                fullyQualifiedNamespace, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable(ServerlessTransport.DefaultServiceBusConnectionName,
                originalConnectionString);
        }

        public class Context : ScenarioContext
        {
            int count;
            public int HandlerInvocationCount => count;

            public void HandlerInvoked() => Interlocked.Increment(ref count);
        }

        class FunctionUsingTokenAuth : FunctionEndpointComponent
        {
            public FunctionUsingTokenAuth() => AddTestMessage(new Message());

            public class Handler(Context testContext) : IHandleMessages<Message>
            {
                public Task Handle(Message message, IMessageHandlerContext context)
                {
                    testContext.HandlerInvoked();
                    return Task.CompletedTask;
                }
            }
        }

        class Message : ICommand;
    }
}