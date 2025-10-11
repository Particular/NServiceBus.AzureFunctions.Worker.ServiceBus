using System.Collections.Generic;
using NServiceBus;

public partial class ServerLessOptions
{
    public EndpointConfiguration ConfigureDefaultSendOnlyEndpoint(string endpointName)
    {
        DefaultEndpointName = endpointName;
        defaultSendOnlyEndpoint = new EndpointConfiguration(endpointName);

        defaultSendOnlyEndpoint.SendOnly();

        return defaultSendOnlyEndpoint;
    }

    internal void Apply(MultiEndpointConfiguration mc)
    {
        foreach (var function in functions)
        {
            mc.AddEndpoint(function);
        }

        if (defaultSendOnlyEndpoint != null)
        {
            mc.AddEndpoint(defaultSendOnlyEndpoint);
        }
    }

    EndpointConfiguration ConfigureEndpoint(string endpointName)
    {
        var endpointConfiguration = new EndpointConfiguration(endpointName);

        functions.Add(endpointConfiguration);

        return endpointConfiguration;
    }

    internal string DefaultEndpointName;
    EndpointConfiguration defaultSendOnlyEndpoint;
    readonly List<EndpointConfiguration> functions = [];
}