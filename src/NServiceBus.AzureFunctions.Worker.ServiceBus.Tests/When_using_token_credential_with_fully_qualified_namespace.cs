namespace NServiceBus.AzureFunctions.Worker.ServiceBus.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Azure.Messaging.ServiceBus;
    using NUnit.Framework;
    using static System.Environment;

    public class When_using_token_credential_with_fully_qualified_namespace
    {
        string fullyQualifiedNamespace;
        string originalConnectionString;
        readonly string fullyQualifiedNamespaceStringKey = $"{defaultConnectionStringKey}__fullyQualifiedNamespace";
        static readonly string defaultConnectionStringKey = ServerlessTransport.DefaultServiceBusConnectionName;

        [SetUp]
        public void SetUp()
        {
            originalConnectionString = GetEnvironmentVariable(defaultConnectionStringKey);

            var connectionStringProperties = ServiceBusConnectionStringProperties.Parse(originalConnectionString);
            fullyQualifiedNamespace = connectionStringProperties.FullyQualifiedNamespace;

            SetEnvironmentVariable(defaultConnectionStringKey, null, EnvironmentVariableTarget.Process);
            SetEnvironmentVariable(fullyQualifiedNamespaceStringKey,
                fullyQualifiedNamespace, EnvironmentVariableTarget.Process);
        }

        [Test]
        public async Task Should_work()
        {
            Context context = await Scenario.Define<Context>()
                .WithComponent(new FunctionUsingTokenCredential())
                .Done(c => c.HandlerInvocationCount > 0)
                .Run();

            Assert.That(context.HandlerInvocationCount, Is.EqualTo(1));
        }

        [TearDown]
        public void TearDown()
        {
            SetEnvironmentVariable(fullyQualifiedNamespaceStringKey,
                fullyQualifiedNamespace, EnvironmentVariableTarget.Process);
            SetEnvironmentVariable(ServerlessTransport.DefaultServiceBusConnectionName,
                originalConnectionString);
        }

        public class Context : ScenarioContext
        {
            int count;
            public int HandlerInvocationCount => count;

            public void HandlerInvoked() => Interlocked.Increment(ref count);
        }

        class FunctionUsingTokenCredential : FunctionEndpointComponent
        {
            public FunctionUsingTokenCredential() => AddTestMessage(new Message());

            class Handler(Context testContext) : IHandleMessages<Message>
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