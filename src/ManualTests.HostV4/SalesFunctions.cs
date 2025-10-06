using NServiceBus;

public class SalesFunctions : IConfigureEndpoint
{
    public void Configure(EndpointConfiguration endpointConfiguration)
    {
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();
        endpointConfiguration.UseTransport(new LearningTransport());
    }
}