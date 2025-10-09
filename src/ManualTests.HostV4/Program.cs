using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using NServiceBus;

var builder = FunctionsApplication.CreateBuilder(args);

// This will be used for 1 function or multiple, ie we will always run in multi endpoint mode even though there is only one function
builder.UseNServiceBus(options =>
{
    //using nameof
    var ordersEndpoint = options.ConfigureEndpoint(nameof(SalesFunctions.Orders));

    // we need an analyzer to prevent new AzureServiceBusTranport() to be used
    var routing = ordersEndpoint.UseTransport(new AzureServiceBusServerlessTransport(TopicTopology.Default));
    routing.RouteToEndpoint(typeof(TriggerMessage), "orders");

    //using source generated method
    var crmIntegrationEndpoint = options.ConfigureCRMIntegrationEndpoint();

    crmIntegrationEndpoint.UseTransport(new AzureServiceBusServerlessTransport(TopicTopology.Default));
// Optionally users can ask for a send onlky endpoint to be used, this endpoint will be a send-onlu and injectable as IMessageSession / ITransactionalSession  
//    c.ConfigureDefaultSendOnlyEndpoint(c =>
//    {
//        //This approach where users now call .UseTransport is more aligned with standard NSB config and also give users stronly typed access to transport settings without us having to invent settings to expose them
//        // in this mode we would need an analyzer to prevent users from using the AzureServiceBusTransport here
//        var globalRouting = c.UseTransport(new AzureServiceBusServerlessTransport(TopicTopology.Default));
//
//        c.SendOnly();
//    })
//        
});

var host = builder.Build();

await host.RunAsync();


// the other approach would be to inherit from the ASB transport to auto expose all properties and methods