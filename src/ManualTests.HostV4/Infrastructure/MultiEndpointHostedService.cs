using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NServiceBus;
using NServiceBus.AzureFunctions.Worker.ServiceBus;

public class MultiEndpointHostedService(IServiceProvider serviceProvider, IStartableMultiEndpointWithExternallyManagedContainer startable, ILoggerFactory loggerFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var nserviceBusLogger = loggerFactory.CreateLogger("NServiceBus");

        FunctionsLoggerFactory.Instance.SetCurrentLogger(nserviceBusLogger);

        multiEndpointInstance = await startable.Start(serviceProvider, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default) => await multiEndpointInstance.Stop(cancellationToken);

    IMultiEndpointInstance multiEndpointInstance;
}