using NServiceBus;

class EndpointRegistry
{
    public static void RegisterEndpoints(MultiEndpointConfiguration multiEndpointConfiguration)
    {
        // codegen
        IConfigureEndpoint salesFunctions = new SalesFunctions();
        var sales = multiEndpointConfiguration.AddEndpoint("sales");

        salesFunctions.Configure(sales);
    }
}