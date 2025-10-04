using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NServiceBus;

public class MultiEndpointHostedService(IServiceProvider serviceProvider, IStartableMultiEndpointWithExternallyManagedContainer startable) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken = default) => multiEndpointInstance = await startable.Start(serviceProvider, cancellationToken);

    public async Task StopAsync(CancellationToken cancellationToken = default) => await multiEndpointInstance.Stop(cancellationToken);

    IMultiEndpointInstance multiEndpointInstance;
}