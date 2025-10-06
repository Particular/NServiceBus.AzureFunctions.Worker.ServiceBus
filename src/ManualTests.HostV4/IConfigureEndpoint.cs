using NServiceBus;

interface IConfigureEndpoint
{
    void Configure(EndpointConfiguration endpointConfiguration);
}