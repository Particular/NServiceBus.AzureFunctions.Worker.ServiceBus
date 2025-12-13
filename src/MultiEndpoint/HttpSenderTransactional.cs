using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NServiceBus.TransactionalSession;

class HttpSenderTransactional([FromKeyedServices("SenderEndpoint")] ITransactionalSession session, ILogger<HttpSender> logger)
{
    [Function("HttpSenderV4Transactional")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestData req,
        FunctionContext executionContext)
    {
        logger.LogInformation("C# HTTP trigger function received a request.");

        await session.Send( "ReceiverEndpoint", new TriggerMessage()).ConfigureAwait(false);

        var r = req.CreateResponse(HttpStatusCode.OK);
        await r.WriteStringAsync($"{nameof(TriggerMessage)} sent.")
            .ConfigureAwait(false);
        return r;
    }
}