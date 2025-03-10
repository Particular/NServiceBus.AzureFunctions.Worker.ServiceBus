﻿namespace NServiceBus.AzureFunctions.Worker.ServiceBus.Tests
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting.Customization;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.AzureFunctions.Worker.ServiceBus;

    class DefaultEndpoint : IEndpointSetupTemplate
    {
        public async Task<EndpointConfiguration> GetConfiguration(
            RunDescriptor runDescriptor,
            EndpointCustomizationConfiguration endpointConfiguration,
            Func<EndpointConfiguration, Task> configurationBuilderCustomization)
        {
            var configuration = new EndpointConfiguration(endpointConfiguration.EndpointName);

            configuration.TypesToIncludeInScan(endpointConfiguration.GetTypesScopedByTestClass());
            configuration.EnableInstallers();

            configuration.RegisterComponents(c => c
                .AddSingleton(runDescriptor.ScenarioContext.GetType(), runDescriptor.ScenarioContext));

            var recoverability = configuration.Recoverability();
            recoverability.Delayed(delayed => delayed.NumberOfRetries(0));
            recoverability.Immediate(immediate => immediate.NumberOfRetries(0));
            configuration.SendFailedMessagesTo("error");

            var connectionString = Environment.GetEnvironmentVariable(ServerlessTransport.DefaultServiceBusConnectionName);
            var topology = TopicTopology.Default;
            topology.OverrideSubscriptionNameFor(endpointConfiguration.EndpointName, endpointConfiguration.EndpointName.Shorten());
            var azureServiceBusTransport = new AzureServiceBusTransport(connectionString, topology);

            _ = configuration.UseTransport(azureServiceBusTransport);

            _ = configuration.UseSerialization<SystemJsonSerializer>();

            await configurationBuilderCustomization(configuration);

            return configuration;
        }
    }
}