namespace ServiceBus.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Messaging.ServiceBus.Administration;
    using Microsoft.Extensions.Hosting;
    using NServiceBus;
    using NServiceBus.AzureFunctions.Worker.ServiceBus;
    using NServiceBus.Features;
    using NUnit.Framework;

    [TestFixture]
    public class When_starting_the_function_without_installers
    {
        [Test]
        public async Task Should_not_create_queues()
        {
            string functionName = "f" + Guid.NewGuid().ToString();

            var host = new HostBuilder()
            //.ConfigureFunctionsWorkerDefaults() NOTE cannot use outside an Azure Function project without too much infrastructure setup
            .UseNServiceBus(
                functionName,
                (con, c) =>
                {
                    c.AdvancedConfiguration.DisableFeature<Sagas>();
                })
            .Build();

            await host.StartAsync();
            Thread.Sleep(5000);
            await host.StopAsync();

            var connectionString = Environment.GetEnvironmentVariable(ServerlessTransport.DefaultServiceBusConnectionName);
            Assert.IsNotNull(connectionString, $"Environment variable '{ServerlessTransport.DefaultServiceBusConnectionName}' should be defined to run tests.");

            var client = new ServiceBusAdministrationClient(connectionString);

            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            Assert.IsFalse(await client.QueueExistsAsync(functionName, cancellationTokenSource.Token), "Queues should not be created");
        }
    }
}