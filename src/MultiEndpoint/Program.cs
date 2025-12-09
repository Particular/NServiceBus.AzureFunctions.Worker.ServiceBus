using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Hosting;
using NServiceBus.AzureFunctions.Worker.ServiceBus;
using NServiceBus.TransactionalSession;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Sender();
builder.Receiver();

// builder.AddNServiceBus(c =>
// {
//     c.Routing.RouteToEndpoint(typeof(TriggerMessage), "FunctionsTestEndpoint2");
//     c.AdvancedConfiguration.EnableInstallers();
// });

var host = builder.Build();

await host.RunAsync();

public static class SenderEndpointConfigurationExtensions
{
    public static void Sender(this FunctionsApplicationBuilder builder)
    {
        builder.Services.AddAzureClientsCore();

        var endpointConfiguration = new EndpointConfiguration("SenderEndpoint");
        endpointConfiguration.SendOnly();
        var persistence = endpointConfiguration.UsePersistence<MongoPersistence>();
        persistence.EnableTransactionalSession(new TransactionalSessionOptions
        {
            ProcessorEndpoint = "ReceiverEndpoint"
        });
        var transport = new AzureServiceBusTransport("TransportWillBeInitializedCorrectlyLater", TopicTopology.Default);
        endpointConfiguration.UseTransport(new ServerlessTransport(transport, null, "SeviceBusConnection"));
    }
}

public static class ReceiverEndpointConfigurationExtensions
{
    public static void Receiver(this FunctionsApplicationBuilder builder)
    {
        builder.Services.AddAzureClientsCore();

        var endpointConfiguration = new EndpointConfiguration("ReceiverEndpoint");
        var persistence = endpointConfiguration.UsePersistence<MongoPersistence>();
        persistence.EnableTransactionalSession();
    }
}