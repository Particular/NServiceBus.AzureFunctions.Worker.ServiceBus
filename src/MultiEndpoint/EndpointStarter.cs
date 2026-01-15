using MultiEndpoint.Logging;
using MultiEndpoint.Services;
using NServiceBus.AzureFunctions.Worker.ServiceBus;

sealed class EndpointStarter(
    IStartableEndpointWithExternallyManagedContainer startableEndpoint,
    IServiceProvider serviceProvider,
    AzureServiceBusServerlessTransport azureServiceBusServerlessTransport,
    string serviceKey,
    KeyedServiceCollectionAdapter services) : IAsyncDisposable
{
    public string ServiceKey => serviceKey;

    public async ValueTask<IEndpointInstance> GetOrStart(CancellationToken cancellationToken = default)
    {
        if (endpoint != null)
        {
            return endpoint;
        }

        await startSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (endpoint != null)
            {
                return endpoint;
            }

            keyedServices = new KeyedServiceProviderAdapter(serviceProvider, serviceKey, services);
            azureServiceBusServerlessTransport.ServiceProvider = keyedServices;

            using var scope = FunctionsLoggerFactory.Instance.PushName(ServiceKey);
            scope.Flush();

            endpoint = await startableEndpoint.Start(keyedServices, cancellationToken).ConfigureAwait(false);

            return endpoint;
        }
        finally
        {
            startSemaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (endpoint == null || keyedServices == null)
        {
            return;
        }

        using var scope = FunctionsLoggerFactory.Instance.PushName(ServiceKey);
        if (endpoint != null)
        {
            await endpoint.Stop().ConfigureAwait(false);
        }

        if (keyedServices != null)
        {
            await keyedServices.DisposeAsync().ConfigureAwait(false);
        }
        startSemaphore.Dispose();
    }

    readonly SemaphoreSlim startSemaphore = new(1, 1);

    IEndpointInstance? endpoint;
    KeyedServiceProviderAdapter? keyedServices;
}