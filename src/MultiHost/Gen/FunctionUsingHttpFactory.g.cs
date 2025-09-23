namespace MultiHost;

using System.ComponentModel;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;

public partial class FunctionUsingHttpFactory : IConfigureEndpoint
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    readonly FunctionEndpoint endpoint;
    readonly IMessageSession session;

    public FunctionUsingHttpFactory(IHttpClientFactory httpClientFactory,
        [FromKeyedServices(nameof(Sales))] FunctionEndpoint endpoint,
        [FromKeyedServices(nameof(Sales))] IMessageSession session)
        : this(httpClientFactory)
    {
        this.endpoint = endpoint;
        this.session = session;
    }

    public FunctionUsingHttpFactory() { }

    // We could make these partial and register on the root DI. Then you can inject stuff into Configure
    public class SalesInitializationContext : InitializationContext;

    public async partial Task Sales(ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, CancellationToken cancellationToken = default) =>
        await endpoint.Process(message, messageActions, cancellationToken).ConfigureAwait(false);

    partial void Configure(SalesInitializationContext context);

    public void Configure(InitializationContext context)
    {
        switch (context)
        {
            case SalesInitializationContext salesContext:
                Configure(salesContext);
                break;
            default:
                break;
        }
    }
}