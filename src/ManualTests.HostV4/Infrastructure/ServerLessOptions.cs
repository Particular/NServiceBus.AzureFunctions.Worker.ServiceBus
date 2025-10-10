using System.Collections.Generic;
using NServiceBus;

public partial class ServerLessOptions
{
    public EndpointConfiguration ConfigureDefaultSendOnlyEndpoint(string endpointName)
    {
        var endpointConfiguration = ConfigureEndpoint(endpointName);

        endpointConfiguration.SendOnly();

        return endpointConfiguration;
    }

    internal void Apply(MultiEndpointConfiguration mc)
    {
        foreach (var function in functions)
        {
            mc.AddEndpoint(function);
        }
    }

    EndpointConfiguration ConfigureEndpoint(string endpointName)
    {
        var endpointConfiguration = new EndpointConfiguration(endpointName);

        functions.Add(endpointConfiguration);

        return endpointConfiguration;
    }

    readonly List<EndpointConfiguration> functions = [];
}