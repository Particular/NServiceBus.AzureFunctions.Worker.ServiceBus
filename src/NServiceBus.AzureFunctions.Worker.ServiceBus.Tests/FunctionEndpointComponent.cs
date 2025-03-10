namespace NServiceBus.AzureFunctions.Worker.ServiceBus.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Messaging.ServiceBus;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Customization;
    using NServiceBus.AcceptanceTesting.Support;
    using Transport.AzureServiceBus;
    using Conventions = AcceptanceTesting.Customization.Conventions;

    abstract class FunctionEndpointComponent : IComponentBehavior
    {
        public Task<ComponentRunner> CreateRunner(RunDescriptor runDescriptor) =>
            Task.FromResult<ComponentRunner>(
                new FunctionRunner(
                    testMessages,
                    CustomizeConfiguration,
                    OnStartCore,
                    runDescriptor.ScenarioContext,
                    PublisherMetadata,
                    GetType()));

        public Action<ServiceBusTriggeredEndpointConfiguration> CustomizeConfiguration { private get; set; } = _ => { };

        public PublisherMetadata PublisherMetadata { get; } = new PublisherMetadata();

        public void AddTestMessage(object body, IDictionary<string, object> userProperties = null) =>
            testMessages.Add(new TestMessage
            {
                Body = body,
                UserProperties = userProperties ?? new Dictionary<string, object>()
            });

        protected virtual Task OnStart(IFunctionEndpoint functionEndpoint, FunctionContext functionContext) => Task.CompletedTask;

        Task OnStartCore(IFunctionEndpoint functionEndpoint, FunctionContext functionContext) => OnStart(functionEndpoint, functionContext);

        IList<TestMessage> testMessages = [];

        class FunctionRunner(
            IList<TestMessage> messages,
            Action<ServiceBusTriggeredEndpointConfiguration> configurationCustomization,
            Func<IFunctionEndpoint, FunctionContext, Task> onStart,
            ScenarioContext scenarioContext,
            PublisherMetadata publisherMetadata,
            Type functionComponentType)
            : ComponentRunner
        {
            public override string Name { get; } = Conventions.EndpointNamingConvention(functionComponentType);

            public override async Task Start(CancellationToken cancellationToken = default)
            {
                var hostBuilder = Host.CreateDefaultBuilder();
                hostBuilder.ConfigureServices(services =>
                {
                    // TODO Think about using a real logger or the NServiceBus.Testing logging infrastructure?
                    services.AddSingleton<ILoggerFactory>(new TestLoggingFactory());
                });
                hostBuilder.UseNServiceBus(Name, (configuration, triggerConfiguration) =>
                {
                    var endpointConfiguration = triggerConfiguration.AdvancedConfiguration;

                    endpointConfiguration.TypesToIncludeInScan(functionComponentType.GetTypesScopedByTestClass());

                    if (triggerConfiguration.Transport.Topology is TopicPerEventTopology topology)
                    {
                        topology.OverrideSubscriptionNameFor(Name, Name.Shorten());

                        foreach (var eventType in publisherMetadata.Publishers.SelectMany(p => p.Events))
                        {
                            topology.PublishTo(eventType, eventType.ToTopicName());
                            topology.SubscribeTo(eventType, eventType.ToTopicName());
                        }
                    }

                    endpointConfiguration.EnforcePublisherMetadataRegistration(Name, publisherMetadata);

                    endpointConfiguration.Recoverability()
                        .Immediate(i => i.NumberOfRetries(0))
                        .Delayed(d => d.NumberOfRetries(0))
                        .Failed(c => c
                            // track messages sent to the error queue to fail the test
                            .OnMessageSentToErrorQueue((failedMessage, ct) =>
                            {
                                _ = scenarioContext.FailedMessages.AddOrUpdate(
                                    Name,
                                    [failedMessage],
                                    (_, fm) =>
                                    {
                                        var failedMessages = fm.ToList();
                                        failedMessages.Add(failedMessage);
                                        return failedMessages;
                                    });
                                return Task.CompletedTask;
                            }));

                    endpointConfiguration.RegisterComponents(c => c.AddSingleton(scenarioContext.GetType(), scenarioContext));

                    // enable installers to auto-create the input queue for tests
                    // in real Azure functions the input queue is assumed to exist
                    endpointConfiguration.EnableInstallers();

                    configurationCustomization(triggerConfiguration);
                });

                host = hostBuilder.Build();
                await host.StartAsync(cancellationToken);

                endpoint = host.Services.GetRequiredService<IFunctionEndpoint>();
            }

            public override async Task ComponentsStarted(CancellationToken cancellationToken = default)
            {
                await onStart(endpoint, new FakeFunctionContext { InstanceServices = host.Services });
                foreach (var message in messages)
                {
                    var userProperties = MessageHelper.GetUserProperties(message.Body);

                    foreach (var customUserProperty in message.UserProperties)
                    {
                        userProperties[customUserProperty.Key] = customUserProperty.Value;
                    }

                    var functionContext = new FakeFunctionContext { InstanceServices = host.Services };
                    var messageActions = new FakeServiceBusMessageActions();
                    var serviceBusReceivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
                        MessageHelper.GetBody(message.Body), properties: userProperties,
                        messageId: Guid.NewGuid().ToString("N"), deliveryCount: 1);
                    await endpoint.Process(serviceBusReceivedMessage, messageActions, functionContext, cancellationToken);
                }
            }

            public override async Task Stop(CancellationToken cancellationToken = default)
            {
                await host.StopAsync(cancellationToken);

                if (scenarioContext.FailedMessages.TryGetValue(Name, out var failedMessages))
                {
                    throw new MessageFailedException(failedMessages.First(), scenarioContext);
                }
            }

            IFunctionEndpoint endpoint;
            IHost host;
        }

        class TestMessage
        {
            public object Body;
            public IDictionary<string, object> UserProperties;
        }
    }
}