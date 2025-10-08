using System;
using System.Collections.Generic;
using NServiceBus;

class EndpointRegistry
{
    public void RegisterEndpoints(MultiEndpointConfiguration multiEndpointConfiguration, Action<EndpointConfiguration> commonConfiguration = null)
    {
        foreach (var function in GetFunctions())
        {
            var endpointConfiguration = multiEndpointConfiguration.AddEndpoint(function.Name);

            //TODO: Set queue name?
            //TODO: Configure correct transport
            endpointConfiguration.UseTransport(new LearningTransport());

            commonConfiguration?.Invoke(endpointConfiguration);

            function.Configuration?.Invoke(endpointConfiguration);
        }
    }

    IEnumerable<FunctionConfiguration> GetFunctions()
    {
        // codegen
        return [new FunctionConfiguration(nameof(SalesFunctions.Orders)), new FunctionConfiguration(nameof(SalesFunctions.CRMIntegration))];
    }

    record FunctionConfiguration(string Name, Action<EndpointConfiguration> Configuration = null);
}