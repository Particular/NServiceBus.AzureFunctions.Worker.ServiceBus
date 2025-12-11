using Microsoft.Extensions.Hosting;

sealed class NServiceBusHostedService(EndpointStarter endpointStarter) : IHostedService, IAsyncDisposable
{
    public async Task StartAsync(CancellationToken cancellationToken = default)
        => await endpointStarter.GetOrStart(cancellationToken).ConfigureAwait(false);

    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await endpointStarter.DisposeAsync();
    }
}