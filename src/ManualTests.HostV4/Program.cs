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
    // We need a dedicated handler registration api
    //crmIntegrationEndpoint.RegisterHandler<TriggerMessage, TriggerMessageHandler>();


    var defaultEndpoint = options.ConfigureDefaultSendOnlyEndpoint("MyFunctionApp");

    defaultEndpoint.UseSerialization<SystemJsonSerializer>();
    var defaultEndpointRouting = defaultEndpoint.UseTransport(new AzureServiceBusServerlessTransport(TopicTopology.Default));

    defaultEndpointRouting.RouteToEndpoint(typeof(TriggerMessage), "orders"); // should we add some stronly typed routing helpers here since me know the address or the servicebustriggers
});

await builder.Build().RunAsync();