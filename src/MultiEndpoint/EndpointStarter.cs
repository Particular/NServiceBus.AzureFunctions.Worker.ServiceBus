using MultiEndpoint.Services;
using NServiceBus.AzureFunctions.Worker.ServiceBus;

sealed class EndpointStarter(
    IStartableEndpointWithExternallyManagedContainer startableEndpoint,
    IServiceProvider serviceProvider,
    ServerlessTransport serverlessTransport,
    string serviceKey,
    KeyedServiceCollectionAdapter services) : IAsyncDisposable
{
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

            // LogManager.UseFactory(new LoggerFactory(loggerFactory));
            // deferredLoggerFactory.FlushAll(loggerFactory);

            keyedServices = new KeyedServiceProviderAdapter(serviceProvider, serviceKey, services);
            serverlessTransport.ServiceProvider = keyedServices;

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