namespace MultiHost;

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public partial class SalesFunction
{
    [Function(nameof(SalesFunction))]
    // Currently we are bluntly keeing against the nameof(Method) but maybe we should key against the function name instead?
    // Also for the endpoint name should we treat the function name as the endpoint name and override the queue name to be the one in the trigger
    // for better clarity?
    public partial Task Sales(
        [ServiceBusTrigger("sales", Connection = "ServiceBusConnection", AutoCompleteMessages = false)]
        Azure.Messaging.ServiceBus.ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions, FunctionContext context, CancellationToken cancellationToken = default);

    partial void Configure(SalesInitializationContext context)
    {
        // Daniel this is is very low ceremony and not a lot of boiler plate code. The benefits of this outweights
        // any auto open approach that the functions host currently implements because that requires still to do
        // Assembly.GetEntryAssembly().GetTypes() which has impacts on trimming and startup time
        //
        // an additional benefit of having an explicit API call is that when there are special cases we can extend the generated code
        // with more options. For example when someone wants all discovered handlers but has a cross cutting handler that doesn't need to be added
        // for this one they could do something like this: context.AddHandlers(options => options.Exclude<MyCrossCuttingHandler>()); or however that
        // API might look like
        context.AddHandlers();
        context.AddSagas();

        context.Routing.RouteToEndpoint(typeof(PlaceOrder), "sales");
    }

    // The drawback of having this thing here is that we need to share CTOR args with the user and that might lead to troubles when they want to inject their own stuff
    [Function("SalesAPI")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
        HttpRequestData request,
        FunctionContext context)
    {
        // with TX session enabled, these 3 operations would be "atomic"
        await session.Send(new PlaceOrder()).ConfigureAwait(false);
        //await session.Send(new SomeOtherMessage()).ConfigureAwait(false);
        //await cosmosDB.SaveStuff(new Order());

        return request.CreateResponse(HttpStatusCode.OK);
    }
}