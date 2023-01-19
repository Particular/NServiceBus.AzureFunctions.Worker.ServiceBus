namespace ServiceBus.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Customization;
    using NServiceBus.AcceptanceTesting.Support;
    using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

    abstract class FunctionEndpointComponent : IComponentBehavior
    {
        public Task<ComponentRunner> CreateRunner(RunDescriptor runDescriptor)
        {
            return Task.FromResult<ComponentRunner>(
                new FunctionRunner(
                    testMessages,
                    CustomizeConfiguration,
                    runDescriptor.ScenarioContext,
                    GetType()));
        }

        public Action<ServiceBusTriggeredEndpointConfiguration> CustomizeConfiguration { private get; set; } = _ => { };

        public void AddTestMessage(object body, IDictionary<string, object> userProperties = null)
        {
            testMessages.Add(new TestMessage
            {
                Body = body,
                UserProperties = userProperties ?? new Dictionary<string, object>()
            });
        }

        IList<TestMessage> testMessages = new List<TestMessage>();

        class FunctionRunner : ComponentRunner
        {
            public FunctionRunner(
                IList<TestMessage> messages,
                Action<ServiceBusTriggeredEndpointConfiguration> configurationCustomization,
                ScenarioContext scenarioContext,
                Type functionComponentType)
            {
                this.messages = messages;
                this.configurationCustomization = configurationCustomization;
                this.scenarioContext = scenarioContext;
                this.functionComponentType = functionComponentType;
                Name = Conventions.EndpointNamingConvention(functionComponentType);
            }

            public override string Name { get; }

            public override Task Start(CancellationToken token)
            {
                var functionEndpointConfiguration = new ServiceBusTriggeredEndpointConfiguration(Name, null, default);
                var endpointConfiguration = functionEndpointConfiguration.AdvancedConfiguration;

                endpointConfiguration.TypesToIncludeInScan(functionComponentType.GetTypesScopedByTestClass());

                endpointConfiguration.Recoverability()
                    .Immediate(i => i.NumberOfRetries(0))
                    .Delayed(d => d.NumberOfRetries(0))
                    .Failed(c => c
                        // track messages sent to the error queue to fail the test
                        .OnMessageSentToErrorQueue((failedMessage, ct) =>
                        {
                            scenarioContext.FailedMessages.AddOrUpdate(
                                Name,
                                new[] { failedMessage },
                                (_, fm) =>
                                {
                                    var messages = fm.ToList();
                                    messages.Add(failedMessage);
                                    return messages;
                                });
                            return Task.CompletedTask;
                        }));

                endpointConfiguration.RegisterComponents(c => c.AddSingleton(scenarioContext.GetType(), scenarioContext));

                // enable installers to auto-create the input queue for tests
                // in real Azure functions the input queue is assumed to exist
                endpointConfiguration.EnableInstallers();

                configurationCustomization(functionEndpointConfiguration);

                var serverless = functionEndpointConfiguration.MakeServerless();

                var serviceCollection = new ServiceCollection();
                var startableEndpointWithExternallyManagedContainer = EndpointWithExternallyManagedContainer.Create(functionEndpointConfiguration.AdvancedConfiguration, serviceCollection);
                var serviceProvider = serviceCollection.BuildServiceProvider();

                endpoint = new FunctionEndpoint(startableEndpointWithExternallyManagedContainer, serverless, serviceProvider);

                return Task.CompletedTask;
            }

            public override async Task ComponentsStarted(CancellationToken token)
            {
                foreach (var message in messages)
                {
                    var userProperties = MessageHelper.GetUserProperties(message.Body);

                    foreach (var customUserProperty in message.UserProperties)
                    {
                        userProperties[customUserProperty.Key] = customUserProperty.Value;
                    }

                    var functionContext = new FakeFunctionContext();
                    await endpoint.Process(
                        MessageHelper.GetBody(message.Body),
                        userProperties,
                        Guid.NewGuid().ToString("N"),
                        1,
                        null,
                        string.Empty,
                        functionContext,
                        token);
                }
            }

            public override Task Stop()
            {
                if (scenarioContext.FailedMessages.TryGetValue(Name, out var failedMessages))
                {
                    throw new MessageFailedException(failedMessages.First(), scenarioContext);
                }

                return base.Stop();
            }

            readonly Action<ServiceBusTriggeredEndpointConfiguration> configurationCustomization;
            readonly ScenarioContext scenarioContext;
            readonly Type functionComponentType;
            IList<TestMessage> messages;
            FunctionEndpoint endpoint;
        }

        class TestMessage
        {
            public object Body;
            public IDictionary<string, object> UserProperties;
        }
    }
}