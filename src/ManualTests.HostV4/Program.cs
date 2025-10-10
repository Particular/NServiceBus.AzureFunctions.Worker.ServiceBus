using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using NServiceBus;

var builder = FunctionsApplication.CreateBuilder(args);

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

    defaultEndpointRouting.RouteToEndpoint(typeof(TriggerMessage), "orders"); // should we add some strongly typed routing helpers here since me know the address of the servicebus triggers
});

await builder.Build().RunAsync();