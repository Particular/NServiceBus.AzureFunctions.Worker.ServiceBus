using System.Collections.Generic;
using NServiceBus;

public partial class ServerLessOptions
{
    public void Apply(MultiEndpointConfiguration mc)
    {
        foreach (var function in functions)
        {
            mc.AddEndpoint(function);
        }
    }

    public EndpointConfiguration ConfigureEndpoint(string endpointName)
    {
        var endpointConfiguration = new EndpointConfiguration(endpointName);

        functions.Add(endpointConfiguration);

        return endpointConfiguration;
    }

    readonly List<EndpointConfiguration> functions = [];
}