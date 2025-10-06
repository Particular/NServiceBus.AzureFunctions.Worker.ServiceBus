using NServiceBus;

public class SalesFunctions : IConfigureEndpoint
{
    public void Configure(EndpointConfiguration endpointConfiguration)
    {
        endpointConfiguration.UseTransport(new LearningTransport());
    }
}