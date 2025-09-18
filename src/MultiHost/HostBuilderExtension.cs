namespace MultiHost;

using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddNServiceBus(this IServiceCollection services, Action<EndpointConfiguration>? customize = null)
    {
        var endpointConfiguration = new EndpointConfiguration("NotEvenUsed");
        // we still have the same problem with the transport seam not being DI aware and have to make similar workarounds
        // unless we create an options type that has to be kept in sync with the transport configuration API
        var transport = new AzureServiceBusTransport("TransportWillBeInitializedCorrectlyLater", TopicTopology.Default);
        endpointConfiguration.UseTransport(transport);

        var recoverability = endpointConfiguration.Recoverability();
        recoverability.Immediate(settings => settings.NumberOfRetries(5));
        recoverability.Delayed(settings => settings.NumberOfRetries(3));

        customize?.Invoke(endpointConfiguration);

        services.AddSingleton(new CommonEndpointConfigurationProvider(endpointConfiguration));
    }
}

class CommonEndpointConfigurationProvider(EndpointConfiguration commonEndpointConfiguration)
{
    public EndpointConfiguration CommonEndpointConfiguration { get; init; } = commonEndpointConfiguration;
}