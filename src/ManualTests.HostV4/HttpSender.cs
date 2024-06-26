﻿using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NServiceBus;


class HttpSender(IFunctionEndpoint functionEndpoint)
{
    [Function("HttpSenderV4")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestData req,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger<HttpSender>();
        logger.LogInformation("C# HTTP trigger function received a request.");

        await functionEndpoint.Send(new TriggerMessage(), executionContext)
            .ConfigureAwait(false);

        var r = req.CreateResponse(HttpStatusCode.OK);
        await r.WriteStringAsync($"{nameof(TriggerMessage)} sent.")
            .ConfigureAwait(false);
        return r;
    }
}
