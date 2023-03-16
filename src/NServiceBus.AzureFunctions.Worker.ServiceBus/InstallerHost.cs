using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NServiceBus.Installation;

namespace NServiceBus;

class InstallerHost : IHostedService
{
    InstallerWithExternallyManagedContainer installer;
    IServiceProvider serviceProvider;

    public InstallerHost(InstallerWithExternallyManagedContainer installer, IServiceProvider serviceProvider)
    {
        this.installer = installer;
        this.serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return installer.Setup(serviceProvider, cancellationToken);
        //return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}