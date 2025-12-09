namespace MultiHost;

using Sales;

[NServiceBusEndpointFunction("Finance", QueueName = "finance-queue")]
public partial class LessBoilerplateFunction
{
    // Rather than an attribute, NServiceBusEndpointFunction could also be a base class
    // making it easier to require the Configure as an abstract method, though it would
    // still need to be partial class to allow the function method and initialization
    // context to be added in

    // Endpoint name could be optional as well and default to class name, falling back to
    // specific value

    // Queue name could be specific provided value, falling back to endpoint name,
    // falling back (as endpoint name does) to class name

    // That means you could potentially just have [NServiceBusEndpointFunction] on a class

    partial void Configure(LessBoilerplateFunctionInitializationContext context)
    {
        context.Routing.RouteToEndpoint(typeof(PlaceOrder), "sales");
    }
}