using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NServiceBus;

class HttpSender(IMessageSession defaultSession, [FromKeyedServices("sales")] IMessageSession salesSession, ILogger<HttpSender> logger)
{
    [Function("HttpSenderV4")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
        HttpRequestData req)
    {
        logger.LogInformation("C# HTTP trigger function received a request.");

        await salesSession.SendLocal(new TriggerMessage());

        await defaultSession.SendLocal(new TriggerMessage());

        var r = req.CreateResponse(HttpStatusCode.OK);
        await r.WriteStringAsync($"{nameof(TriggerMessage)} sent.")
            .ConfigureAwait(false);
        return r;
    }
}