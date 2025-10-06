using System;
using NServiceBus;

class EndpointRegistry
{
    public static void RegisterEndpoints(MultiEndpointConfiguration multiEndpointConfiguration, Action<EndpointConfiguration> commonConfiguration = null)
    {
        // codegen
        IConfigureEndpoint salesFunctions = new SalesFunctions();
        var salesConfiguration = multiEndpointConfiguration.AddEndpoint("sales");

        commonConfiguration?.Invoke(salesConfiguration);

        salesFunctions.Configure(salesConfiguration);
    }
}