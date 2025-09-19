namespace MultiHost;

using Microsoft.Extensions.DependencyInjection;

public abstract class InitializationContext
{
    public AzureServiceBusTransport Transport { get; init; }
    public EndpointConfiguration Configuration { get; init; }
    public RoutingSettings<AzureServiceBusTransport> Routing { get; init; }
    public IServiceCollection Services { get; init; }
}