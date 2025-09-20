namespace MultiHost;

using System.Net;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;

public partial class SalesFunction
{
    [Function(nameof(SalesFunction))]
    public partial Task Sales(
        [ServiceBusTrigger("sales", Connection = "ServiceBusConnection", AutoCompleteMessages = false)]
Azure.Messaging.ServiceBus.ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, CancellationToken cancellationToken = default);

    partial void Configure(SalesInitializationContext context)
    {
        context.Configuration.AddHandler<PlaceOrderHandler>();
        context.Configuration.AddSaga<OrderFullfillmentPolicy>();

        context.Routing.RouteToEndpoint(typeof(PlaceOrder), "sales");
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