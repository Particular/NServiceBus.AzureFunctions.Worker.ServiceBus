namespace MultiHost;

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public partial class FunctionUsingHttpFactory
{
    readonly IHttpClientFactory httpClientFactory;

    public FunctionUsingHttpFactory(IHttpClientFactory httpClientFactory)
    {
        this.httpClientFactory = httpClientFactory;
    }

    [Function(nameof(FunctionUsingHttpFactory))]
    // Currently we are bluntly keeing against the nameof(Method) but maybe we should key against the function name instead?
    // Also for the endpoint name should we treat the function name as the endpoint name and override the queue name to be the one in the trigger
    // for better clarity?
    public partial Task Sales(
        [ServiceBusTrigger("blah", Connection = "ServiceBusConnection", AutoCompleteMessages = false)]
        Azure.Messaging.ServiceBus.ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, FunctionContext context, CancellationToken cancellationToken = default);

    partial void Configure(SalesInitializationContext context)
    {
        // Not important to show constructor capability
    }

    // The drawback of having this thing here is that we need to share CTOR args with the user and that might lead to troubles when they want to inject their own stuff
    [Function("SomeApi")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
        HttpRequestData request,
        FunctionContext executionContext)
    {
        // with TX session enabled, these 3 operations would be "atomic"
        await session.Send(new PlaceOrder()).ConfigureAwait(false);
        //await session.Send(new SomeOtherMessage()).ConfigureAwait(false);
        //await cosmosDB.SaveStuff(new Order());

        // Can use HTTP factory from the constructor
        _ = httpClientFactory.CreateClient();

        return request.CreateResponse(HttpStatusCode.OK);
    }
}