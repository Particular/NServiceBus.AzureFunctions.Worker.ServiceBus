namespace ServiceBus.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Customization;
    using NServiceBus.AcceptanceTesting.Support;
    using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

    abstract class SimpleTriggerFunctionComponent : IComponentBehavior
    {
        public Task<ComponentRunner> CreateRunner(RunDescriptor runDescriptor) =>
            Task.FromResult<ComponentRunner>(
                new FunctionRunner(
                    CustomizeConfiguration,
                    runDescriptor.ScenarioContext,
                    GetType(),
                    TriggerAction));

        public abstract Task TriggerAction(IFunctionEndpoint endpoint, FunctionContext context);

        public Action<ServiceBusTriggeredEndpointConfiguration> CustomizeConfiguration { private get; set; } = _ => { };

        class FunctionRunner : ComponentRunner
        {
            public FunctionRunner(Action<ServiceBusTriggeredEndpointConfiguration> configurationCustomization,
                ScenarioContext scenarioContext,
                Type functionComponentType,
                Func<IFunctionEndpoint, FunctionContext, Task> triggerAction)
            {
                this.configurationCustomization = configurationCustomization;
                this.scenarioContext = scenarioContext;
                this.functionComponentType = functionComponentType;
                this.triggerAction = triggerAction;

                Name = Conventions.EndpointNamingConvention(functionComponentType);
            }

            public override string Name { get; }

            public override Task Start(CancellationToken token)
            {
                var functionEndpointConfiguration = new ServiceBusTriggeredEndpointConfiguration(Name, default, null);
                var endpointConfiguration = functionEndpointConfiguration.AdvancedConfiguration;

                endpointConfiguration.TypesToIncludeInScan(functionComponentType.GetTypesScopedByTestClass());

                endpointConfiguration.Recoverability()
                    .Immediate(i => i.NumberOfRetries(0))
                    .Delayed(d => d.NumberOfRetries(0));

                configurationCustomization(functionEndpointConfiguration);

                endpointConfiguration.RegisterComponents(c => c.AddSingleton(scenarioContext.GetType(), scenarioContext));

                //endpointConfiguration.RegisterComponents(c => c.AddSingleton<IMutateOutgoingTransportMessages>(b => new TestIndependenceMutator(scenarioContext)));

                var serviceCollection = new ServiceCollection();
                var endpointFactory = functionEndpointConfiguration.CreateEndpointFactory(serviceCollection);

                var serviceProvider = serviceCollection.BuildServiceProvider();

                endpoint = endpointFactory.Invoke(serviceProvider);

                return Task.CompletedTask;
            }

            public override async Task ComponentsStarted(CancellationToken cancellationToken)
            {
                await triggerAction(endpoint, new FakeFunctionContext());
                await base.ComponentsStarted(cancellationToken);
            }

            IFunctionEndpoint endpoint;

            readonly Action<ServiceBusTriggeredEndpointConfiguration> configurationCustomization;
            readonly ScenarioContext scenarioContext;
            readonly Type functionComponentType;
            readonly Func<IFunctionEndpoint, FunctionContext, Task> triggerAction;
        }
    }
}