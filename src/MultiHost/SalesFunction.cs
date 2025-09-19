namespace MultiHost;

using System.Net;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.AzureFunctions.Worker.ServiceBus;
using NServiceBus.Configuration.AdvancedExtensibility;

public partial class SalesFunction
{
    [Function(nameof(SalesFunction))]
    public partial Task Sales(
        [ServiceBusTrigger("sales", Connection = "ServiceBusConnection", AutoCompleteMessages = false)]
Azure.Messaging.ServiceBus.ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, CancellationToken cancellationToken = default);

    public void Configure(AzureServiceBusTransport transport, RoutingSettings<AzureServiceBusTransport> routing, EndpointConfiguration endpointConfiguration)
    {
        endpointConfiguration.AddHandler<PlaceOrderHandler>();
        endpointConfiguration.AddSaga<OrderFullfillmentPolicy>();

        routing.RouteToEndpoint(typeof(PlaceOrder), "sales");
    }

    // The drawback of having this thing here is that we need to share CTOR args with the user and that might lead to troubles when they want to inject their own stuff
    [Function("SalesAPI")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
        HttpRequestData request,
        FunctionContext executionContext)
    {
        // with TX session enabled, these 3 operations would be "atomic"
        await session.Send(new PlaceOrder()).ConfigureAwait(false);
        //await session.Send(new SomeOtherMessage()).ConfigureAwait(false);
        //await cosmosDB.SaveStuff(new Order());

        return request.CreateResponse(HttpStatusCode.OK);
    }
}

public partial class BillingFunctions
{
    [Function(nameof(Billing))]
    public partial Task Billing(
        [ServiceBusTrigger("billing", Connection = "ServiceBusConnection2", AutoCompleteMessages = false)] //Using a separate namespace would need the bridge for the individual endpoints to talk to each other
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, CancellationToken cancellationToken = default);

    partial void Configure(BillingInitializationContext context)
    {
        context.Configuration.AddHandler<OrderAcceptedHandler>();
    }

    [Function(nameof(Invoices))]
    public partial Task Invoices(
        [ServiceBusTrigger("invoices", Connection = "ServiceBusConnection2", AutoCompleteMessages = false)] //Using a separate namespace would need the bridge for the individual endpoints to talk to each other
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, CancellationToken cancellationToken = default);

    partial void Configure(InvoicesInitializationContext context)
    {
        context.Configuration.AddHandler<OrderAcceptedHandler>();
    }
}

public partial class BillingFunctions : IConfigureEndpoint
{
    readonly FunctionEndpoint billing;
    readonly FunctionEndpoint invoices;

    public BillingFunctions([FromKeyedServices(nameof(Billing))] FunctionEndpoint billing, [FromKeyedServices(nameof(Invoices))] FunctionEndpoint invoices)
    {
        this.billing = billing;
        this.invoices = invoices;
    }

    // for configuration
    public BillingFunctions()
    {
    }

    public class BillingInitializationContext : InitializationContext;

    public async partial Task Billing(ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, CancellationToken cancellationToken = default)
    {
        await billing.Process(message, messageActions, cancellationToken).ConfigureAwait(false);
    }

    partial void Configure(BillingInitializationContext context);

    public class InvoicesInitializationContext : InitializationContext;

    public async partial Task Invoices(ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, CancellationToken cancellationToken = default)
    {
        await invoices.Process(message, messageActions, cancellationToken).ConfigureAwait(false);
    }

    partial void Configure(InvoicesInitializationContext context);

    public void Configure(InitializationContext context)
    {
        switch (context)
        {
            case BillingInitializationContext billingContext:
                Configure(billingContext);
                break;
            case InvoicesInitializationContext invoicesContext:
                Configure(invoicesContext);
                break;
            default:
                break;
        }
    }
}

public abstract class InitializationContext
{
    public AzureServiceBusTransport Transport { get; init; }
    public EndpointConfiguration Configuration { get; init; }
    public RoutingSettings<AzureServiceBusTransport> Routing { get; init; }
    public IServiceCollection Services { get; init; }
}

public partial class SalesFunction : IConfigureEndpoint
{
    readonly FunctionEndpoint endpoint;
    readonly IMessageSession session;

    public SalesFunction([FromKeyedServices(nameof(Sales))] FunctionEndpoint endpoint, [FromKeyedServices(nameof(SalesFunction))] IMessageSession session)
    {
        this.endpoint = endpoint;
        this.session = session;
    }

    public SalesFunction() {}

    public class SalesInitializationContext : InitializationContext;

    public async partial Task Sales(ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, CancellationToken cancellationToken = default) =>
        // Because we generate this you can essentially add a decorator chain of IConfigureEndpoint implementations
        // that for example adds specific stuff to the endpoint configuration like source generator discovered handlers
        // and other things.
        await endpoint.Process(message, messageActions, cancellationToken).ConfigureAwait(false);

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

public interface IConfigureEndpoint
{
    void Configure(InitializationContext context);
}

// In the current version this object does send and receive but technically we could now totally split this responsibility
// and have a FunctionReceiver and a FunctionSender or just use IMessageSession with a sendonly endpoint or do we foresee
// specific routing being necessary per function?
public sealed class FunctionEndpoint(Func<ServiceBusReceivedMessage, ServiceBusMessageActions, CancellationToken, Task> processor)
{
    public async Task Process(ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions,
        CancellationToken cancellationToken = default)
    {
        await processor(message, messageActions, cancellationToken).ConfigureAwait(false);
        await Console.Out
            .WriteLineAsync($"Processing message: {message.MessageId}").ConfigureAwait(false);
    }
}