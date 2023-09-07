namespace ServiceBus.Tests
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Messaging.ServiceBus.Administration;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using NServiceBus;
    using NServiceBus.Features;
    using NUnit.Framework;

    [TestFixture]
    public class When_starting_the_function_with_installers
    {
        [Test]
        public async Task Should_create_queues()
        {
            string functionName = "f" + Guid.NewGuid().ToString();

            var host = new HostBuilder()
            //.ConfigureFunctionsWorkerDefaults() NOTE cannot use outside an Azure Function project without too much infrastructure setup
            .UseNServiceBus(
                functionName,
                (con, c) =>
                {
                    c.AdvancedConfiguration.DisableFeature<Sagas>();
                    c.AdvancedConfiguration.EnableInstallers();
                })
            .Build();

            await host.StartAsync();
            Thread.Sleep(5000);
            await host.StopAsync();

            var configBuilder = new ConfigurationBuilder();
            configBuilder.SetBasePath(Directory.GetCurrentDirectory());
            configBuilder.AddEnvironmentVariables();
            configBuilder.AddJsonFile("local.settings.json", true);
            var config = configBuilder.Build();

            var connectionString = config.GetValue<string>("AzureWebJobsServiceBus") ?? config.GetValue<string>("Values:AzureWebJobsServiceBus");
            Assert.IsNotNull(connectionString, "Environment variable 'AzureWebJobsServiceBus' should be defined to run tests.");

            var client = new ServiceBusAdministrationClient(connectionString);

            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            Assert.IsTrue(await client.QueueExistsAsync(functionName, cancellationTokenSource.Token), "Queue should have been created");

            await client.DeleteQueueAsync(functionName, cancellationTokenSource.Token);
        }
    }
}