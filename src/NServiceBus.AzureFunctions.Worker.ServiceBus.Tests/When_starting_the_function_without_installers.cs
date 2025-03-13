namespace NServiceBus.AzureFunctions.Worker.ServiceBus.Tests;

using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus.Administration;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AzureFunctions.Worker.ServiceBus;
using NServiceBus.Configuration.AdvancedExtensibility;
using NUnit.Framework;
using Conventions = AcceptanceTesting.Customization.Conventions;

[TestFixture]
public class When_starting_the_function_without_installers
{
    [SetUp]
    public async Task SetUp()
    {
        var connectionString = Environment.GetEnvironmentVariable(ServerlessTransport.DefaultServiceBusConnectionName);
        Assert.That(connectionString, Is.Not.Null, $"Environment variable '{ServerlessTransport.DefaultServiceBusConnectionName}' should be defined to run tests.");

        adminClient = new ServiceBusAdministrationClient(connectionString);

        endpointNamingConvention = Conventions.EndpointNamingConvention(typeof(FunctionWithoutInstallersEnabled));
        // This is to make sure when this test is executed locally it would still pass. On CI/CD we have an ASB instance for each run
        if (await adminClient.QueueExistsAsync(endpointNamingConvention))
        {
            await adminClient.DeleteQueueAsync(endpointNamingConvention);
        }
    }

    [Test]
    public async Task Should_not_create_queues()
    {
        await Scenario.Define<ScenarioContext>()
            .WithComponent(new FunctionWithoutInstallersEnabled())
            .Done(c => c.EndpointsStarted)
            .Run();

        Assert.That((bool)await adminClient.QueueExistsAsync(endpointNamingConvention), Is.False, "Queues should not be created");
    }

    class FunctionWithoutInstallersEnabled : FunctionEndpointComponent
    {
        public FunctionWithoutInstallersEnabled() =>
            CustomizeConfiguration = c =>
            {
                // The base infrastructure enables installers by default. There is no official way to disable installers
                // so we have to use the backdoor settings key to disable them.
                c.AdvancedConfiguration.GetSettings().Set("Installers.Enable", false);
            };
    }

    ServiceBusAdministrationClient adminClient;
    string endpointNamingConvention;
}