using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using NServiceBus;

var builder = FunctionsApplication.CreateBuilder(args);

// This will be used for 1 function or multiple, ie we will always run in multi endpoint mode even though there is only one function
builder.UseNServiceBus(options =>
{
    var ordersEndpoint = options.ConfigureOrdersEndpoint();

    ordersEndpoint.UseSerialization<SystemJsonSerializer>();
    // We need to validate that users don't call send only
    //ordersEndpoint.SendOnly();

    // we need an analyzer to prevent new AzureServiceBusTransport() to be used
    var routing = ordersEndpoint.UseTransport(new AzureServiceBusServerlessTransport(TopicTopology.Default));
    routing.RouteToEndpoint(typeof(TriggerMessage), "orders");

    var crmIntegrationEndpoint = options.ConfigureCRMIntegrationEndpoint();

    crmIntegrationEndpoint.UseSerialization<SystemJsonSerializer>();
    crmIntegrationEndpoint.UseTransport(new AzureServiceBusServerlessTransport(TopicTopology.Default));
    //crmIntegrationEndpoint.RegisterHandler<TriggerMessage, THandler>();


    var defaultEndpoint = options.ConfigureDefaultSendOnlyEndpoint("MyFunctionAppEndpoint");

    defaultEndpoint.UseSerialization<SystemJsonSerializer>();
    defaultEndpoint.UseTransport(new AzureServiceBusServerlessTransport(TopicTopology.Default));
});

var host = builder.Build();

await host.RunAsync();


// the other approach would be to inherit from the ASB transport to auto expose all properties and methods