namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;
    using Installation;

    class InstallerHost : IHostedService
    {
        readonly FunctionEndpoint functionEndpoint;

        public InstallerHost(IFunctionEndpoint functionEndpoint)
        {
            this.functionEndpoint = functionEndpoint as FunctionEndpoint;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return functionEndpoint.InitializeEndpointIfNecessary(cancellationToken);
            //return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}