using NServiceBus;

public partial class ServerLessOptions
{
    public EndpointConfiguration ConfigureCRMIntegrationEndpoint()
    {
        return this.ConfigureEndpoint(nameof(SalesFunctions.CRMIntegration));
    }
}