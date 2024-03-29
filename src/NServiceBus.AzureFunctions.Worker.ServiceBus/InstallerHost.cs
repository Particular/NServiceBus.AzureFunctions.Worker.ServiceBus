﻿namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;

    class InstallerHost : IHostedService
    {
        readonly FunctionEndpoint functionEndpoint;

        public InstallerHost(IFunctionEndpoint functionEndpoint)
        {
            this.functionEndpoint = functionEndpoint as FunctionEndpoint;
        }

        public Task StartAsync(CancellationToken cancellationToken = default) => functionEndpoint.InitializeEndpointIfNecessary(cancellationToken);

        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}